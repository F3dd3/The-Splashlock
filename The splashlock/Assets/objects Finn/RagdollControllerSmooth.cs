using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class RagdollControllerSmooth : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Ragdoll Settings")]
    public float ragdollDuration = 5f;
    public float blendDuration = 2f;

    [Header("Bone Reference (optional)")]
    public Transform mainBone; // leave empty for auto-detect

    [Header("Smooth Follow Settings")]
    public bool smoothRootFollow = true;       // root follows ragdoll
    public float followSpeed = 5f;             // Lerp speed
    public bool rotateRootWithRagdoll = true;  // root rotates with ragdoll

    [HideInInspector] public Rigidbody[] ragdollRigidbodies;
    [HideInInspector] public bool isRagdoll = false;

    private Transform[] bones;
    private Vector3[] savedLocalPositions;
    private Quaternion[] savedLocalRotations;
    private Vector3[] originalLocalPositions;
    private Quaternion[] originalLocalRotations;

    private Vector3 originalRootPosition;
    private Quaternion originalRootRotation;
    private Vector3 boneOffset;

    private Rigidbody rootRigidbody;
    private Collider rootCollider;

    void Awake()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        bones = new Transform[ragdollRigidbodies.Length];
        for (int i = 0; i < ragdollRigidbodies.Length; i++)
            bones[i] = ragdollRigidbodies[i].transform;

        if (mainBone == null)
            mainBone = FindCentralBone();

        rootRigidbody = GetComponent<Rigidbody>();
        rootCollider = GetComponent<Collider>();

        if (mainBone != null)
            boneOffset = transform.position - mainBone.position;

        SetRagdoll(false);
    }

    void Update()
    {
        if (isRagdoll && mainBone != null && smoothRootFollow)
        {
            Vector3 targetPos = mainBone.position + boneOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

            if (rotateRootWithRagdoll)
                transform.rotation = Quaternion.Slerp(transform.rotation, mainBone.rotation, Time.deltaTime * followSpeed);
        }
    }

    public void ActivateRagdoll()
    {
        if (animator == null || mainBone == null)
        {
            Debug.LogWarning("[RagdollControllerSmooth] Animator or mainBone null - cannot ragdoll.");
            return;
        }

        originalRootPosition = transform.position;
        originalRootRotation = transform.rotation;

        originalLocalPositions = new Vector3[bones.Length];
        originalLocalRotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            originalLocalPositions[i] = bones[i].localPosition;
            originalLocalRotations[i] = bones[i].localRotation;
        }

        isRagdoll = true;
        SetRagdoll(true);

        boneOffset = transform.position - mainBone.position;

        StartCoroutine(ReturnToAnimationAfterDelay(ragdollDuration));
    }

    private IEnumerator ReturnToAnimationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isRagdoll)
            StartCoroutine(BlendBackToOriginalPose());
    }

    private IEnumerator BlendBackToOriginalPose()
    {
        // 1) Save current ragdoll pose
        savedLocalPositions = new Vector3[bones.Length];
        savedLocalRotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            savedLocalPositions[i] = bones[i].localPosition;
            savedLocalRotations[i] = bones[i].localRotation;
        }

        // 2) Disable ragdoll physics (keep CC in sync)
        SetRagdoll(false);

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.detectCollisions = false;
        if (rootCollider != null) rootCollider.enabled = false;

        // Reset animator
        animator.Rebind();
        animator.Update(0f);

        // 3) Calculate ragdoll end transform (position + upright rotation)
        Vector3 ragdollEndPos = mainBone.position + boneOffset;
        Quaternion uprightRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f); // only Y rotation
        Quaternion ragdollEndRot = uprightRotation;

        float timer = 0f;
        while (timer < blendDuration)
        {
            float t = timer / blendDuration;

            // Blend bones
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = Vector3.Lerp(savedLocalPositions[i], originalLocalPositions[i], t);
                bones[i].localRotation = Quaternion.Slerp(savedLocalRotations[i], originalLocalRotations[i], t);
            }

            // Smoothly rotate root upright
            transform.rotation = Quaternion.Slerp(transform.rotation, ragdollEndRot, t);

            timer += Time.deltaTime;
            yield return null;
        }

        // 4) Ground snap using capsulecast
        Vector3 snapPos = ragdollEndPos;
        float skinOffset = 0.08f;
        float castDistance = 2f;

        float capsuleRadius = 0.3f;
        float capsuleHeight = 1.8f;

        if (cc != null)
        {
            capsuleRadius = cc.radius * 0.95f;
            capsuleHeight = cc.height;
        }

        Vector3 top = snapPos + Vector3.up * (capsuleHeight * 0.5f - capsuleRadius);
        Vector3 bottom = snapPos + Vector3.up * capsuleRadius;

        if (Physics.CapsuleCast(top, bottom, capsuleRadius, Vector3.down, out RaycastHit hit, castDistance))
        {
            transform.position = hit.point + Vector3.up * skinOffset;
            Debug.Log($"[RagdollControllerSmooth] Capsule-snapped to {hit.point}, normal {hit.normal}");
        }
        else
        {
            transform.position = snapPos + Vector3.up * 0.1f;
            Debug.LogWarning("[RagdollControllerSmooth] Ground capsulecast failed; fallback applied.");
        }

        // 5) Wait one physics frame
        yield return new WaitForFixedUpdate();

        // 6) Re-enable colliders / CC
        if (cc != null) cc.detectCollisions = true;
        if (rootCollider != null) rootCollider.enabled = true;

        // 7) Final animator resync
        animator.Rebind();
        yield return null;
        animator.Update(Time.deltaTime);

        isRagdoll = false;
        Debug.Log("[RagdollControllerSmooth] Finished blending back to animation safely.");
    }





    public void SetRagdoll(bool active)
    {
        if (animator != null)
            animator.enabled = !active;

        if (rootRigidbody != null)
            rootRigidbody.isKinematic = !active;

        // ⚠️ Laat root collider aan tijdens ragdoll
        // if (rootCollider != null)
        //     rootCollider.enabled = !active;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            rb.isKinematic = !active;
            rb.detectCollisions = active;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // ✅ Zorg dat physics direct up-to-date zijn
        Physics.SyncTransforms();
    }


    private Transform FindCentralBone()
    {
        if (ragdollRigidbodies == null || ragdollRigidbodies.Length == 0)
            return transform;

        Transform best = null;
        int bestCount = -1;

        foreach (var rb in ragdollRigidbodies)
        {
            int count = rb.GetComponentsInChildren<Rigidbody>().Length;
            if (count > bestCount)
            {
                best = rb.transform;
                bestCount = count;
            }
        }

        return best;
    }
}
