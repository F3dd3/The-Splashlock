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

    [Header("Slow/Smash Settings")]
    public float slowCheckDistance = 1f;
    private bool onSlowSurface = false;
    public float smashForce = 12f;   // kracht van smash

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

        CheckGrounded();

        float slopeAngle = 0f;
        if (grounded)
            slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);

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

        // --- Speed bepalen ---
        float currentSpeed = moveSpeed;
        if (onSlowSurface)
            currentSpeed = slowSpeed;

        Vector3 horizontalMove = Vector3.zero;

        if (grounded)
        {
            if (slopeAngle > slopeLimit)
            {
                float slopeFactor = Mathf.InverseLerp(slopeLimit, 90f, slopeAngle);
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
                horizontalMove = slideDirection * slopeFactor * slopeSlideSpeed;
            }
            else
            {
                Vector3 moveDir = Vector3.ProjectOnPlane(moveInput, groundHit.normal).normalized;
                float slopeSpeedFactor = Mathf.Lerp(1f, 0.2f, slopeAngle / slopeLimit);
                horizontalMove = moveDir * (currentSpeed * slopeSpeedFactor);
            }
        }
        else
        {
            horizontalMove = moveInput * currentSpeed;
        }

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

        Vector3 finalMove = horizontalMove + new Vector3(0, velocity.y, 0);
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

        // Extra check voor Slow surfaces iets verder weg
        if (!onSlowSurface && Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit extraHit, slowCheckDistance))
        {
            if (extraHit.collider.CompareTag("Slow"))
                onSlowSurface = true;
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Smash wordt nu alleen getriggerd door daadwerkelijke collision
        if (hit.collider.CompareTag("Smash"))
        {
            Vector3 center = hit.collider.bounds.center;
            Vector3 knockDir = (transform.position - center).normalized;

            float distance = Vector3.Distance(transform.position, center);
            float boost = Mathf.Clamp(distance, 1f, 2f);

            Vector3 force = knockDir * smashForce * boost;
            force.y = Mathf.Max(force.y, smashForce * 0.5f);

            velocity += force;
        }
    }

    public void SetVerticalVelocity(float newVelocity)
    {
        velocity.y = newVelocity;
        lastJumpTime = Time.time;
    }
}
