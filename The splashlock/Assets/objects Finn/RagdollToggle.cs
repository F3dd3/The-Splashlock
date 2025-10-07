using UnityEngine;

public class RagdollController : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    private Rigidbody[] ragdollRigidbodies;

    void Awake()
    {
        // Vind alle rigidbodies in dit object (en kinderen)
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();

        // Zet ragdoll uit bij start
        SetRagdoll(false);
    }

    void Update()
    {
        // Linkermuisknop drukt ragdoll aan
        if (Input.GetMouseButtonDown(0))
        {
            SetRagdoll(true);
        }
    }

    void SetRagdoll(bool active)
    {
        // Animator uit als ragdoll actief is
        if (animator != null)
            animator.enabled = !active;

        // Zet alle rigidbodies aan/uit
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = !active; // false = physics aan
            rb.detectCollisions = active;
        }
    }
}


