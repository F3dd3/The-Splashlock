using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RagdollControllerSmooth))]
[RequireComponent(typeof(CharacterMovement))]
public class SpikeTrigger : MonoBehaviour
{
    private RagdollControllerSmooth ragdollController;
    private CharacterMovement movement;

    void Awake()
    {
        // Vind de componenten op dezelfde GameObject
        ragdollController = GetComponent<RagdollControllerSmooth>();
        movement = GetComponent<CharacterMovement>();

        if (ragdollController == null)
            Debug.LogError("RagdollControllerSmooth niet gevonden!");
        if (movement == null)
            Debug.LogError("CharacterMovement niet gevonden!");
    }

    // Dit werkt voor trigger colliders
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Spike"))
        {
            TriggerRagdoll();
        }
    }

    // Dit werkt voor normale colliders (Collision)
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Spike"))
        {
            TriggerRagdoll();
        }
    }

    private void TriggerRagdoll()
    {
        if (ragdollController != null && !ragdollController.isRagdoll)
        {
            // 1️⃣ Stop speler input tijdelijk
            if (movement != null)
                movement.enabled = false;

            // 2️⃣ Activeer ragdoll
            ragdollController.ActivateRagdoll();

            // 3️⃣ Movement automatisch weer inschakelen na ragdoll + blend
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
