using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RagdollControllerSmooth))]
public class RagdollActivator : MonoBehaviour
{
    private RagdollControllerSmooth ragdollController;
    private CharacterController characterController;

    [Header("Scripts to disable while ragdolling (auto filled if empty)")]
    public MonoBehaviour[] scriptsToToggle;

    [Header("Pushback instellingen")]
    public Vector3 pushBackForce = new Vector3(0, 0, -2f);

    [Header("Immunity")]
    public float ragdollImmunityDuration = 0.5f;
    private bool isRagdollActive = false;
    private bool ragdollImmune = false;

    void Start()
    {
        ragdollController = GetComponent<RagdollControllerSmooth>();
        characterController = GetComponent<CharacterController>();

        // auto-fill scriptsToToggle met common movement scripts als niet ingesteld
        if (scriptsToToggle == null || scriptsToToggle.Length == 0)
        {
            var list = new System.Collections.Generic.List<MonoBehaviour>();
            var move = GetComponent<CharacterMovement_Local>();
            if (move != null) list.Add(move);
            var anim = GetComponent<PlayerAnimation_Local>();
            if (anim != null) list.Add(anim);
            // voeg deze script (RagdollActivator) niet toe
            scriptsToToggle = list.ToArray();
        }

        // Zorg dat controller vanaf begin aanstaat
        if (characterController) characterController.enabled = true;

        // root rigidbody kinematic (als aanwezig)
        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    public void EnableRagdoll()
    {
        if (isRagdollActive || ragdollImmune) return;

        isRagdollActive = true;

        // 1) disable movement/anim scripts and controller
        if (characterController) characterController.enabled = false;
        foreach (var s in scriptsToToggle)
            if (s != null) s.enabled = false;

        // 2) activeer ragdoll
        ragdollController.ActivateRagdoll();

        // 3) pushback next frame
        StartCoroutine(PushBackNextFrame());

        // 4) wacht tot ragdoll + blend klaar en restore
        StartCoroutine(RestoreMovementAfterRagdoll());
    }

    private IEnumerator PushBackNextFrame()
    {
        yield return new WaitForEndOfFrame();

        // push meerdere rigidbodies voor een realistischer effect (hips + enkele children)
        if (ragdollController.hipsBone != null)
        {
            // eerst probeer hips rigidbody
            var hipsRb = ragdollController.hipsBone.GetComponent<Rigidbody>();
            if (hipsRb != null)
                hipsRb.AddForce(transform.TransformDirection(pushBackForce), ForceMode.Impulse);
            else
            {
                // fallback: push a few child rigidbodies
                foreach (var rb in ragdollController.ragdollRigidbodies)
                {
                    if (rb == null) continue;
                    rb.AddForce(transform.TransformDirection(pushBackForce * 0.5f), ForceMode.Impulse);
                }
            }
        }
    }

    private IEnumerator RestoreMovementAfterRagdoll()
    {
        // wacht tot ragdoll + blend klaar is
        yield return new WaitForSeconds(ragdollController.ragdollDuration + ragdollController.blendDuration + 0.1f);

        // bepaal waar hips zijn
        if (ragdollController.hipsBone != null)
        {
            Vector3 targetPos = ragdollController.hipsBone.position;
            Vector3 safePos = targetPos;

            // 🔹 RAYCAST-CHECK: zorg dat we niet in een muur of object staan
            if (Physics.Raycast(targetPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 2f))
            {
                // land op grondniveau
                safePos = groundHit.point;
            }

            // 🔹 ANTI-WALL-CLIP: check of we overlappen met colliders
            Collider[] overlaps = Physics.OverlapCapsule(safePos + Vector3.up * 0.5f, safePos + Vector3.up * 1.5f, 0.4f);
            foreach (var col in overlaps)
            {
                if (!col.transform.IsChildOf(transform))
                {
                    // duw iets terug vanaf de muur
                    Vector3 dir = (transform.position - col.ClosestPoint(safePos)).normalized;
                    safePos += dir * 0.3f;
                }
            }

            // controller tijdelijk uitzetten, positie aanpassen, en weer aan
            if (characterController != null) characterController.enabled = false;
            transform.position = safePos;
            if (characterController != null) characterController.enabled = true;
        }

        // scripts weer aanzetten
        foreach (var s in scriptsToToggle)
            if (s != null) s.enabled = true;

        isRagdollActive = false;

        // korte immunity en collision buffer na herstel
        StartCoroutine(RagdollImmunityTimer());
        StartCoroutine(CollisionBufferTimer());
    }



    private IEnumerator RagdollImmunityTimer()
    {
        ragdollImmune = true;
        yield return new WaitForSeconds(ragdollImmunityDuration);
        ragdollImmune = false;
    }

    private IEnumerator CollisionBufferTimer()
    {
        // Zorg dat movement 0.5 seconden uit blijft, zodat collisions goed herstellen
        var move = GetComponent<CharacterMovement_Local>();
        if (move != null) move.enabled = false;

        yield return new WaitForSeconds(0.5f);

        if (move != null) move.enabled = true;
    }

}
