using UnityEngine;

public class TemporaryRagdollTrigger : MonoBehaviour
{
    [Header("Instellingen")]
    [Tooltip("Hoe lang de ragdoll actief mag zijn (in seconden) nadat je klikt).")]
    public float activeDuration = 1f; // tijd dat de trigger aan blijft

    private bool ragdollEnabled = false;
    private float timer = 0f;

    void Update()
    {
        // Klik om de ragdoll trigger tijdelijk te activeren
        if (Input.GetMouseButtonDown(0))
        {
            ragdollEnabled = true;
            timer = activeDuration;
            Debug.Log("TemporaryRagdollTrigger geactiveerd voor " + activeDuration + " seconde(n).");
        }

        // Tel af als de ragdoll aanstaat
        if (ragdollEnabled)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                ragdollEnabled = false;
                Debug.Log("TemporaryRagdollTrigger gedeactiveerd.");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!ragdollEnabled) return;

        RagdollActivator activator = collision.collider.GetComponentInParent<RagdollActivator>();
        if (activator != null)
        {
            activator.EnableRagdoll();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ragdollEnabled) return;

        RagdollActivator activator = other.GetComponentInParent<RagdollActivator>();
        if (activator != null)
        {
            activator.EnableRagdoll();
        }
    }
}
