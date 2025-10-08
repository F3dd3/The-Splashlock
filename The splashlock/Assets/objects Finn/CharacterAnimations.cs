using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterMovement))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private CharacterMovement characterMovement;

    void Start()
    {
        animator = GetComponent<Animator>();
        characterMovement = GetComponent<CharacterMovement>();
    }

    void Update()
    {
        if (animator == null || characterMovement == null) return;

        // Alleen animaties voor lokale speler
        if (!characterMovement.IsOwner) return;

        // Input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveInput = new Vector3(horizontal, 0f, vertical);

        // Run
        bool isRunning = moveInput.magnitude > 0.1f && characterMovement.grounded;
        animator.SetBool("isRunning", isRunning);

        // Jump
        bool isJumping = !characterMovement.grounded && characterMovement.VerticalVelocity > 0.1f;
        animator.SetBool("isJumping", isJumping);

        // Fall
        bool isFalling = !characterMovement.grounded && characterMovement.VerticalVelocity < -0.1f;
        animator.SetBool("isFalling", isFalling);

        // Movement magnitude voor blend tree
        animator.SetFloat("moveSpeed", moveInput.magnitude);
    }
}
