using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class RagdollControllerSmooth : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Instellingen")]
    public float ragdollDuration = 5f;
    public float blendDuration = 2f;

    [Header("Referenties")]
    public Transform hipsBone;

    [HideInInspector] public Rigidbody[] ragdollRigidbodies;
    private Transform[] bones;
    [HideInInspector] public bool isRagdoll = false;

    private Vector3[] savedLocalPositions;
    private Quaternion[] savedLocalRotations;
    private Vector3[] originalLocalPositions;
    private Quaternion[] originalLocalRotations;
    // we keep originalRootPosition for reference but we won't teleport back to it
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

        if (animator != null && hipsBone == null && animator.isHuman)
            hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);

        rootRigidbody = GetComponent<Rigidbody>();
        rootCollider = GetComponent<Collider>();

        SetRagdoll(false);
    }

    void Update() { /* leeg: activatie extern */ }

    public void ActivateRagdoll()
    {
        if (animator == null || hipsBone == null) return;

        // Sla pre-ragdoll pose op (bones lokaal)
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
        // 1) sla ragdoll lokaal op
        savedLocalPositions = new Vector3[bones.Length];
        savedLocalRotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            savedLocalPositions[i] = bones[i].localPosition;
            savedLocalRotations[i] = bones[i].localRotation;
        }

        // 2) zet animator en non-ragdoll state aan (child rigidbodies kinematic)
        SetRagdoll(false);
        animator.Rebind();
        animator.Update(0f);

        // 3) sla huidige ragdoll root positie op (waar de player moet opstaan)
        Vector3 ragdollRootPosition = transform.position;
        Quaternion ragdollRootRotation = transform.rotation;

        // 4) blend lokaal de bone poses terug naar originele pre-ragdoll pose
        float timer = 0f;
        while (timer < blendDuration)
        {
            float t = timer / blendDuration;
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = Vector3.Lerp(savedLocalPositions[i], originalLocalPositions[i], t);
                bones[i].localRotation = Quaternion.Slerp(savedLocalRotations[i], originalLocalRotations[i], t);
            }

            // houd de rootpositie op de ragdoll-locatie tijdens blend
            transform.position = ragdollRootPosition;
            transform.rotation = ragdollRootRotation;

            timer += Time.deltaTime;
            yield return null;
        }

        // 5) forceer animatie pose en zet vlag terug
        animator.Rebind();
        yield return null;
        animator.Update(Time.deltaTime);

        isRagdoll = false;
    }

    public void SetRagdoll(bool active)
    {
        if (animator != null) animator.enabled = !active;

        // root rigidbody blijft kinematic (CharacterController gebruikt)
        if (rootRigidbody != null)
            rootRigidbody.isKinematic = true;

        // keep root collider disabled — child colliders handle physics during ragdoll
        if (rootCollider != null)
            rootCollider.enabled = false;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            rb.isKinematic = !active;
            rb.detectCollisions = active;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
