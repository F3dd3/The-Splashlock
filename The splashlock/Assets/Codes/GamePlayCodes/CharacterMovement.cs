using UnityEngine;
using UnityEngine.UI;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float jumpCooldown = 0.2f; // tijd tussen sprongen als space ingedrukt blijft

    private CharacterController controller;
    private Vector3 velocity;
    private float lastJumpTime;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Shift Lock")]
    public bool shiftLockEnabled = false;

    [Header("Shift Lock UI")]
    public RawImage shiftLockSymbol;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f;
    private bool grounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (shiftLockSymbol != null)
            shiftLockSymbol.enabled = false;
    }

    void Update()
    {
        HandleShiftLock();
        HandleMovementAndGravity();
    }

    void HandleShiftLock()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftLockEnabled = !shiftLockEnabled;
            Cursor.lockState = shiftLockEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shiftLockEnabled;

            if (shiftLockSymbol != null)
                shiftLockSymbol.enabled = shiftLockEnabled;
        }
    }

    void HandleMovementAndGravity()
    {
        // --- Input ---
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * moveZ + right * moveX;
        if (move.magnitude > 1f)
            move.Normalize();

        // --- Ground check ---
        CheckGrounded();

        if (grounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f; // Houd speler strak op de grond

            if (Input.GetButton("Jump") && Time.time - lastJumpTime >= jumpCooldown)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastJumpTime = Time.time;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // --- Rotatie ---
        if (shiftLockEnabled)
        {
            // Kijk exact dezelfde kant als camera
            transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        }
        else if (move.sqrMagnitude > 0.001f)
        {
            // Directe maar vloeiende rotatie (Roblox-style)
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720 * Time.deltaTime);
        }

        // --- Combineer movement en gravity in één Move ---
        Vector3 finalMove = move * moveSpeed + velocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    void CheckGrounded()
    {
        float radius = controller.radius;
        Vector3 origin = transform.position + Vector3.up * (controller.center.y - controller.height / 2 + radius);

        grounded = false; // reset standaard

        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            if (hit.collider.CompareTag("Ground") || hit.collider.CompareTag("Start"))
            {
                grounded = true;
            }
        }

        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, grounded ? Color.green : Color.red);
    }

    // 👇 Handig voor trampolines of bounce pads
    public void SetVerticalVelocity(float newVelocity)
    {
        velocity.y = newVelocity;
        lastJumpTime = Time.time;
    }
}
