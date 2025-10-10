using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TemporaryRagdollTrigger : MonoBehaviour
{
    [Header("Instellingen")]
    [Tooltip("Hoe lang de ragdoll actief mag zijn (in seconden) nadat je klikt).")]
    public float activeDuration = 1f;

    private bool ragdollEnabled = false;
    private float timer = 0f;

    private Transform myRoot;
    private RagdollActivatorNetworked selfRagdoll;

    void Start()
    {
        myRoot = transform.root;
        selfRagdoll = myRoot.GetComponent<RagdollActivatorNetworked>();
    }

    void Update()
    {
        // Stop direct als je zelf ragdolled
        if (selfRagdoll != null && selfRagdoll.isRagdollActive)
        {
            ragdollEnabled = false; // trigger mag niet actief zijn
            return;
        }

        // Klik om de tijdelijke ragdoll trigger te activeren
        if (Input.GetMouseButtonDown(0))
        {
            ragdollEnabled = true;
            timer = activeDuration;
            Debug.Log("TemporaryRagdollTrigger geactiveerd voor " + activeDuration + " seconde(n).");
        }

        // Tel af als ragdoll aanstaat
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
        TryEnableRagdoll(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ragdollEnabled) return;
        TryEnableRagdoll(other);
    }

    private void TryEnableRagdoll(Collider col)
    {
        // Negeer jezelf
        if (col.transform.root == myRoot) return;

        // Haal de Networked RagdollActivator
        RagdollActivatorNetworked activator = col.GetComponentInParent<RagdollActivatorNetworked>();
        if (activator != null)
        {
            // Skip target als die al ragdolled is
            if (!activator.isRagdollActive)
            {
                activator.EnableRagdoll();
            }
        }
    }
}
