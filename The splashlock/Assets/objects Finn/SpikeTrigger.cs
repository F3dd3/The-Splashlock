using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RagdollControllerSmooth))]
[RequireComponent(typeof(CharacterMovement))]
public class SpikeTrigger : MonoBehaviour
{
    private RagdollControllerSmooth ragdollController;
    private CharacterMovement movement;

    void Start()
    {
        ragdollController = GetComponent<RagdollControllerSmooth>();
        movement = GetComponent<CharacterMovement>();

        if (ragdollController == null)
            Debug.LogError("RagdollControllerSmooth niet gevonden op dit object!");
        if (movement == null)
            Debug.LogError("CharacterMovement niet gevonden op dit object!");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Spike"))
        {
            TriggerRagdoll();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spike"))
        {
            TriggerRagdoll();
        }
    }

    private void TriggerRagdoll()
    {
        if (ragdollController != null && !ragdollController.isRagdoll)
        {
            // 1️⃣ Disable player movement input
            if (movement != null) movement.enabled = false;

            // 2️⃣ Activate ragdoll
            ragdollController.ActivateRagdoll();

            // 3️⃣ Re-enable movement after ragdoll duration + blend
            StartCoroutine(ReenableMovementAfterRagdoll());
        }
    }

    private IEnumerator ReenableMovementAfterRagdoll()
    {
        float waitTime = ragdollController.ragdollDuration + ragdollController.blendDuration;

        yield return new WaitForSeconds(waitTime);

        if (movement != null)
            movement.enabled = true;
    }
}
