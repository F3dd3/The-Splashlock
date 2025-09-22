using UnityEngine;
using UnityEngine.UI;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float slowSpeed = 2f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float jumpCooldown = 0.2f;

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
    private RaycastHit groundHit;

    [Header("Slope Settings")]
    public float slopeSlideSpeed = 8f;
    public float slopeLimit = 45f;

    [Header("Slow Settings")]
    public float slowCheckDistance = 1f;
    private bool onSlowSurface = false;

    // Externe kracht (spinners, knockback, enz.)
    private Vector3 externalForce = Vector3.zero;

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

        // Force langzaam laten uitdoven
        if (externalForce.magnitude > 0.01f)
            externalForce = Vector3.Lerp(externalForce, Vector3.zero, 5f * Time.deltaTime);
        else
            externalForce = Vector3.zero;
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

        Vector3 moveInput = forward * moveZ + right * moveX;
        if (moveInput.magnitude > 1f)
            moveInput.Normalize();

        // --- Ground check ---
        CheckGrounded();

        // --- Verticale beweging (gravity/jump) ---
        if (grounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f;

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

        // --- Bepaal huidige snelheid ---
        float currentSpeed = onSlowSurface ? slowSpeed : moveSpeed;

        // --- Horizontale movement ---
        Vector3 horizontalMove = Vector3.zero;

        if (grounded)
        {
            float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);

            if (slopeAngle > slopeLimit)
            {
                // Te steil → glijden
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
                horizontalMove = slideDir * slopeSlideSpeed;
            }
            else
            {
                // Normale beweging, geprojecteerd op de helling
                Vector3 moveDir = Vector3.ProjectOnPlane(moveInput, groundHit.normal).normalized;
                horizontalMove = moveDir * currentSpeed;
            }
        }
        else
        {
            // In de lucht → gewone input
            horizontalMove = moveInput * currentSpeed;
        }

        // --- Rotatie ---
        if (shiftLockEnabled)
        {
            transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        }
        else if (moveInput.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720 * Time.deltaTime);
        }

        // --- Combineer movement met externalForce ---
        Vector3 finalMove = horizontalMove + externalForce + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);
    }

    void CheckGrounded()
    {
        float radius = controller.radius;
        Vector3 origin = transform.position + Vector3.up * (controller.center.y - controller.height / 2 + radius);

        grounded = false;
        onSlowSurface = false;

        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            if (hit.collider.CompareTag("Ground") || hit.collider.CompareTag("Start") || hit.collider.CompareTag("Slow"))
            {
                grounded = true;
                groundHit = hit;

                if (hit.collider.CompareTag("Slow"))
                    onSlowSurface = true;
            }
        }

        if (!onSlowSurface && Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit slowHit, slowCheckDistance))
        {
            if (slowHit.collider.CompareTag("Slow"))
            {
                onSlowSurface = true;
            }
        }

        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, grounded ? Color.green : Color.red);
        Debug.DrawRay(origin, Vector3.down * slowCheckDistance, onSlowSurface ? Color.blue : Color.gray);
    }

    public void SetVerticalVelocity(float newVelocity)
    {
        velocity.y = newVelocity;
        lastJumpTime = Time.time;
    }

    public void AddExternalForce(Vector3 force)
    {
        externalForce += force;
    }
}
