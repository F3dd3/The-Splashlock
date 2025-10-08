using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterMovement))]
public class PlayerAnimation : NetworkBehaviour
{
    private Animator animator;
    private CharacterMovement characterMovement;

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
    }

    void Update()
    {
        if (animator == null || characterMovement == null)
            return;

        if (IsOwner)
        {
            HandleLocalAnimation();
        }

        // Sync met de waarden die over het netwerk komen
        ApplyNetworkedAnimation();
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
    private void ApplyNetworkedAnimation()
    {
        animator.SetBool("isRunning", isRunning.Value);
        animator.SetBool("isJumping", isJumping.Value);
        animator.SetBool("isFalling", isFalling.Value);
        animator.SetFloat("moveSpeed", moveSpeed.Value);
    }
}
