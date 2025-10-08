using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterMovement_Local))]
public class PlayerAnimation_Local : MonoBehaviour
{
    private Animator animator;
    private CharacterMovement_Local characterMovement;

    void Start()
    {
        animator = GetComponent<Animator>();
        characterMovement = GetComponent<CharacterMovement_Local>();
    }

    void Update()
    {
        if (animator == null || characterMovement == null) return;

        // Movement input uitlezen
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveInput = new Vector3(horizontal, 0f, vertical);

        // Lopen/running animatie
        bool isRunning = moveInput.magnitude > 0.01f && characterMovement.controller.isGrounded;
        animator.SetBool("isRunning", isRunning);

        // Spring animatie
        bool isJumping = !characterMovement.controller.isGrounded;
        animator.SetBool("isJumping", isJumping);

        // Optioneel: val animatie
        bool isFalling = characterMovement.controller.velocity.y < -0.1f && !characterMovement.controller.isGrounded;
        animator.SetBool("isFalling", isFalling);
    }
}

