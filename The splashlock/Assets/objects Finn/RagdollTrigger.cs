using UnityEngine;

public class RagdollTrigger : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        RagdollActivator activator = collision.collider.GetComponentInParent<RagdollActivator>();
        if (activator != null)
        {
            activator.EnableRagdoll();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        RagdollActivator activator = other.GetComponentInParent<RagdollActivator>();
        if (activator != null)
        {
            activator.EnableRagdoll();
        }
    }
}
