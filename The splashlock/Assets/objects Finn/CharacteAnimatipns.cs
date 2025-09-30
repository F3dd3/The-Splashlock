using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;

        // Update animator
        bool isRunning = movement.magnitude > 0f;
        animator.SetBool("isRunning", isRunning);

        // Move the player (optional)
        if (isRunning)
        {
            transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
