using UnityEngine;

public class SpikeTrigger : MonoBehaviour
{
    private RagdollControllerSmooth ragdollController;

    void Start()
    {
        // Vind de RagdollControllerSmooth op de speler
        ragdollController = GetComponent<RagdollControllerSmooth>();
        if (ragdollController == null)
            Debug.LogError("RagdollControllerSmooth niet gevonden op dit object!");
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
            ragdollController.ActivateRagdoll();
        }
    }
}
