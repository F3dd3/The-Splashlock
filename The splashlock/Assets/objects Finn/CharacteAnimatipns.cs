using UnityEngine;

public class PlayerAnimation2 : MonoBehaviour
{
    private Animator animator;
    private CharacterMovement characterMovement;

    void Start()
    {
        animator = GetComponent<Animator>();
        characterMovement = GetComponent<CharacterMovement>();

        if (animator == null)
            Debug.LogWarning("Animator niet gevonden op " + gameObject.name);

        if (characterMovement == null)
            Debug.LogWarning("CharacterMovement niet gevonden op " + gameObject.name);
    }

    void Update()
    {

        if (animator == null || characterMovement == null) return;

        // Input uitlezen
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveInput = new Vector3(horizontal, 0f, vertical);

        // Controleer lopen
        bool isRunning = moveInput.magnitude > 0.01f && characterMovement.grounded;
        animator.SetBool("isRunning", isRunning);

        // Controleer springen
        bool isJumping = !characterMovement.grounded; // springt als niet op de grond
        animator.SetBool("isJumping", isJumping);
        Debug.Log($"Input: {moveInput} | Grounded: {characterMovement.grounded} | isRunning: {isRunning}");

    }
}
