using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(OptionsCharacterMovement))]
public class OptionsPlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private OptionsCharacterMovement characterMovement;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterMovement = GetComponent<OptionsCharacterMovement>();
    }

    private void Update()
    {
        if (animator == null || characterMovement == null) return;

        HandleAnimation();
    }

    private void HandleAnimation()
    {
        // Movement input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveInput = new Vector3(horizontal, 0f, vertical);

        // Running = bewegen op de grond
        bool isRunning = moveInput.magnitude > 0.1f && characterMovement.grounded;

        // Jumping / Falling
        float verticalVel = characterMovement.VerticalVelocity;
        bool isJumping = !characterMovement.grounded && verticalVel > 0.1f;
        bool isFalling = !characterMovement.grounded && verticalVel < -0.1f;

        // Update animator parameters
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isFalling", isFalling);
        animator.SetFloat("moveSpeed", moveInput.magnitude);
    }
}
