using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterMovement))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private CharacterMovement characterMovement;
    private CharacterController controller;

    void Start()
    {
        animator = GetComponent<Animator>();
        characterMovement = GetComponent<CharacterMovement>();
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (animator == null || characterMovement == null) return;

        // Enkel animatie updaten voor local player (zoals in movement script)
        if (!characterMovement.IsOwner) return;

        // Input uitlezen
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveInput = new Vector3(horizontal, 0f, vertical);

        // Check of speler beweegt
        bool isRunning = moveInput.magnitude > 0.1f && characterMovement.grounded;
        animator.SetBool("isRunning", isRunning);

        // Springen
        bool isJumping = !characterMovement.grounded && characterMovement.VerticalVelocity > 0.1f;
        animator.SetBool("isJumping", isJumping);

        // Vallen
        bool isFalling = !characterMovement.grounded && characterMovement.VerticalVelocity < -0.1f;
        animator.SetBool("isFalling", isFalling);

        // Optioneel: Snelheid doorgeven aan animator (voor blend tree)
        animator.SetFloat("moveSpeed", moveInput.magnitude);
    }
}

