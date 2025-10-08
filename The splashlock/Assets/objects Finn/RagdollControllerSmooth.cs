using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class RagdollControllerSmooth : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Instellingen")]
    public float ragdollDuration = 5f;   // Hoe lang de ragdoll actief blijft
    public float blendDuration = 2f;     // Hoe lang de blend duurt

    [Header("Referenties")]
    public Transform hipsBone;           // Meestal "Hips" of "Pelvis"

    private Rigidbody[] ragdollRigidbodies;
    private Transform[] bones;
    private bool isRagdoll = false;

    // Huidige ragdoll data
    private Vector3[] savedLocalPositions;
    private Quaternion[] savedLocalRotations;

    // Originele pre-ragdoll data
    private Vector3[] originalLocalPositions;
    private Quaternion[] originalLocalRotations;
    private Vector3 originalRootPosition;
    private Quaternion originalRootRotation;

    private Rigidbody rootRigidbody;
    private Collider rootCollider;

    void Awake()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        bones = new Transform[ragdollRigidbodies.Length];
        for (int i = 0; i < ragdollRigidbodies.Length; i++)
            bones[i] = ragdollRigidbodies[i].transform;

        // Automatische hips detectie
        if (animator != null && hipsBone == null && animator.isHuman)
            hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);

        rootRigidbody = GetComponent<Rigidbody>();
        rootCollider = GetComponent<Collider>();

        SetRagdoll(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isRagdoll)
            ActivateRagdoll();
    }

    private void ActivateRagdoll()
    {
        if (animator == null || hipsBone == null)
        {
            Debug.LogWarning("Animator of hipsBone ontbreekt!");
            return;
        }

        // 1️⃣ Sla originele positie/rotatie op
        originalRootPosition = transform.position;
        originalRootRotation = transform.rotation;

        originalLocalPositions = new Vector3[bones.Length];
        originalLocalRotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            originalLocalPositions[i] = bones[i].localPosition;
            originalLocalRotations[i] = bones[i].localRotation;
        }

        // 2️⃣ Start ragdoll
        isRagdoll = true;
        SetRagdoll(true);
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
        // 1️⃣ Sla ragdoll pose lokaal op
        savedLocalPositions = new Vector3[bones.Length];
        savedLocalRotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            savedLocalPositions[i] = bones[i].localPosition;
            savedLocalRotations[i] = bones[i].localRotation;
        }

        // 2️⃣ Zet animator weer aan
        SetRagdoll(false);
        animator.Rebind();
        animator.Update(0f);

        // 3️⃣ Blend lokaal terug naar originele pre-ragdoll pose
        float timer = 0f;
        while (timer < blendDuration)
        {
            float t = timer / blendDuration;
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = Vector3.Lerp(savedLocalPositions[i], originalLocalPositions[i], t);
                bones[i].localRotation = Quaternion.Slerp(savedLocalRotations[i], originalLocalRotations[i], t);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 4️⃣ Zet root exact terug naar waar hij stond
        transform.position = originalRootPosition;
        transform.rotation = originalRootRotation;

        // 5️⃣ Forceer animatie-pose na herstel
        animator.Rebind();
        yield return null;
        animator.Update(Time.deltaTime);

        isRagdoll = false;
    }

    private void SetRagdoll(bool active)
    {
        if (animator != null)
            animator.enabled = !active;

        if (rootRigidbody != null)
            rootRigidbody.isKinematic = true;

        if (rootCollider != null)
            rootCollider.enabled = !active;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == null) continue;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = !active;
            rb.detectCollisions = active;
        }

        if (!active && animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
