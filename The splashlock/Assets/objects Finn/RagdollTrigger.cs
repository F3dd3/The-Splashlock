using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RagdollTrigger : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        TryEnableRagdoll(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryEnableRagdoll(other);
    }

    private void TryEnableRagdoll(Collider col)
    {
        // Haal de Networked RagdollActivator
        RagdollActivatorNetworked activator = col.GetComponentInParent<RagdollActivatorNetworked>();
        if (activator != null)
        {
            activator.EnableRagdoll();
        }
    }
}
