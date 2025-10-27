using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(RagdollActivatorNetworked))]
public class PlayerAnimation : NetworkBehaviour
{
    private Animator animator;
    private CharacterMovement characterMovement;
    private RagdollActivatorNetworked ragdollActivator;

    // --- Netcode variabelen voor synchronisatie ---
    private NetworkVariable<bool> isRunning = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> isJumping = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> isFalling = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> moveSpeed = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Start()
    {
        animator = GetComponent<Animator>();
        characterMovement = GetComponent<CharacterMovement>();
        ragdollActivator = GetComponent<RagdollActivatorNetworked>();
    }

    void Update()
    {
        if (animator == null || characterMovement == null || ragdollActivator == null)
            return;

        // Check of ragdoll actief is
        bool isRagdoll = ragdollActivator.isRagdollActive;

        // --- Alleen input verwerken als niet ragdolled ---
        if (IsOwner && !isRagdoll)
        {
            HandleLocalAnimation();
        }

        // --- Pas animator waarden alleen toe als niet ragdolled ---
        ApplyNetworkedAnimation(isRagdoll);
    }

    // ------------------ Lokale speler berekent animatie ------------------
    private void HandleLocalAnimation()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveInput = new Vector3(horizontal, 0f, vertical);

        bool running = moveInput.magnitude > 0.1f && characterMovement.grounded;

        float verticalVel = characterMovement.VerticalVelocity;
        bool jumping = !characterMovement.grounded && verticalVel > 0.1f;
        bool falling = !characterMovement.grounded && verticalVel < -0.1f;

        // Update NetworkVariables
        isRunning.Value = running;
        isJumping.Value = jumping;
        isFalling.Value = falling;
        moveSpeed.Value = moveInput.magnitude;
    }

    // ------------------ Wordt door iedereen uitgevoerd ------------------
    private void ApplyNetworkedAnimation(bool isRagdoll)
    {
        if (animator == null) return;

        if (isRagdoll)
        {
            // Als ragdoll actief is, animator uitschakelen
            if (animator.enabled) animator.enabled = false;
        }
        else
        {
            // Heractiveer animator en update parameters
            if (!animator.enabled) animator.enabled = true;

            animator.SetBool("isRunning", isRunning.Value);
            animator.SetBool("isJumping", isJumping.Value);
        }
    }
}
