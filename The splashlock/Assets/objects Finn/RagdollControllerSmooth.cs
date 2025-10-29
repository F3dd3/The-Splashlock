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
            Vector3 rotatedOffset = transform.rotation * boneOffset;
            Vector3 targetPos = mainBone.position + rotatedOffset;

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
        savedLocalPositions = new Vector3[bones.Length];
        savedLocalRotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            savedLocalPositions[i] = bones[i].localPosition;
            savedLocalRotations[i] = bones[i].localRotation;
        }

        SetRagdoll(false);

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.detectCollisions = true;
        if (rootCollider != null) rootCollider.enabled = true;

        animator.Rebind();
        animator.Update(0f);

        Vector3 ragdollEndPos = mainBone.position + boneOffset;
        Quaternion uprightRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Quaternion ragdollEndRot = uprightRotation;

        float timer = 0f;
        while (timer < blendDuration)
        {
            float t = timer / blendDuration;
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = Vector3.Lerp(savedLocalPositions[i], originalLocalPositions[i], t);
                bones[i].localRotation = Quaternion.Slerp(savedLocalRotations[i], originalLocalRotations[i], t);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, ragdollEndRot, t);

            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = ragdollEndPos;

        yield return new WaitForFixedUpdate();

        CharacterMovement move = GetComponent<CharacterMovement>();
        if (move != null) move.enabled = true;
        if (animator != null) animator.enabled = true;

        isRagdoll = false;
        Debug.Log("[RagdollControllerSmooth] Finished blending back to animation safely.");
    }

    public void SetRagdoll(bool active)
    {
        if (animator != null)
            animator.enabled = !active;

        if (rootRigidbody != null)
            rootRigidbody.isKinematic = !active;

        // ✅ Laat CharacterController aan
        CharacterMovement move = GetComponent<CharacterMovement>();
        if (move != null)
            move.enabled = !active;

        if (rootCollider != null)
            rootCollider.enabled = true;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            rb.isKinematic = !active;
            rb.detectCollisions = active;
            rb.useGravity = active;
        }

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
