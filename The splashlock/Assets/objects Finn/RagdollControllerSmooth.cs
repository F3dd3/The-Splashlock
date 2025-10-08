using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class RagdollControllerSmooth : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Instellingen")]
    public float ragdollDuration = 5f;    // hoe lang ragdoll actief is
    public float blendDuration = 2f;      // duur van terugblend

    [Header("Referenties")]
    public Transform hipsBone;             // meestal "Hips" of "Pelvis"

    private Rigidbody[] ragdollRigidbodies;
    private Transform[] bones;
    private bool isRagdoll = false;

    private Vector3[] savedLocalPositions;
    private Quaternion[] savedLocalRotations;

    void Awake()
    {
        // Verzamel alle rigdoll rigidbodies en bones
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        bones = new Transform[ragdollRigidbodies.Length];
        for (int i = 0; i < ragdollRigidbodies.Length; i++)
            bones[i] = ragdollRigidbodies[i].transform;

        // Automatische hips detectie voor humanoids
        if (animator != null && hipsBone == null && animator.isHuman)
            hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);

        SetRagdoll(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isRagdoll)
            ActivateRagdoll();
    }

    private void ActivateRagdoll()
    {
        if (animator == null || hipsBone == null) return;

        isRagdoll = true;
        SetRagdoll(true);
        StartCoroutine(ReturnToAnimationAfterDelay(ragdollDuration));
    }

    private IEnumerator ReturnToAnimationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isRagdoll)
            StartCoroutine(BlendBackToAnimation());
    }

    private IEnumerator BlendBackToAnimation()
    {
        // 1️⃣ Sla ragdoll pose lokaal op
        savedLocalPositions = new Vector3[bones.Length];
        savedLocalRotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            savedLocalPositions[i] = bones[i].localPosition;
            savedLocalRotations[i] = bones[i].localRotation;
        }

        // 2️⃣ Sla ragdoll hips wereldpositie op
        Vector3 ragdollHipsWorldPos = hipsBone.position;
        Quaternion ragdollHipsWorldRot = hipsBone.rotation;

        // 3️⃣ Zet animator terug aan (bevries pose)
        SetRagdoll(false);
        animator.Rebind();
        animator.Update(0f);

        // 4️⃣ Bereken offset om root te verplaatsen zodat hips gelijk blijven
        Vector3 animatorHipsWorldPos = hipsBone.position;
        Quaternion animatorHipsWorldRot = hipsBone.rotation;

        Vector3 positionOffset = ragdollHipsWorldPos - animatorHipsWorldPos;
        Quaternion rotationOffset = ragdollHipsWorldRot * Quaternion.Inverse(animatorHipsWorldRot);

        transform.position += positionOffset;
        transform.rotation = rotationOffset * transform.rotation;

        // 5️⃣ Blend bones lokaal terug naar animator pose
        float timer = 0f;
        while (timer < blendDuration)
        {
            float t = timer / blendDuration;
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = Vector3.Lerp(savedLocalPositions[i], bones[i].localPosition, t);
                bones[i].localRotation = Quaternion.Slerp(savedLocalRotations[i], bones[i].localRotation, t);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 6️⃣ Forceer animator pose na blend zodat hij exact overeenkomt
        animator.Rebind();
        yield return null;             // 1 frame wachten
        animator.Update(Time.deltaTime);

        isRagdoll = false;
    }

    private void SetRagdoll(bool active)
    {
        if (animator != null)
            animator.enabled = !active;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == null) continue;

            rb.isKinematic = !active;
            rb.detectCollisions = active;

            if (!active)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (!active && animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
