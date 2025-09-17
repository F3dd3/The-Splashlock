using UnityEngine;
using UnityEngine.UI;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
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
    public float slopeSlideSpeed = 8f;   // maximale glijsnelheid op 90° helling
    public float slopeLimit = 45f;       // vanaf welke hoek je begint te glijden

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

        Vector3 move = forward * moveZ + right * moveX;
        if (move.magnitude > 1f)
            move.Normalize();

        // --- Ground check ---
        CheckGrounded();

        float slopeSpeedFactor = 1f;

        if (grounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f;

            float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);

            if (Input.GetButton("Jump") && Time.time - lastJumpTime >= jumpCooldown)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastJumpTime = Time.time;
            }
            else
            {
                if (slopeAngle > slopeLimit)
                {
                    // ✅ Schuiven naar beneden (steiler = sneller)
                    float slopeFactor = Mathf.InverseLerp(slopeLimit, 90f, slopeAngle);
                    Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
                    move += slideDirection * slopeFactor * slopeSlideSpeed;
                }
                else
                {
                    // ✅ Langzamer lopen op helling omhoog
                    // projecteer movement op grondvlak
                    Vector3 moveDir = Vector3.ProjectOnPlane(move, groundHit.normal).normalized;

                    // bereken factor afhankelijk van hoek
                    slopeSpeedFactor = Mathf.Lerp(1f, 0.2f, slopeAngle / slopeLimit);

                    move = moveDir;
                }
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // --- Rotatie ---
        if (shiftLockEnabled)
        {
            transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        }
        else if (move.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720 * Time.deltaTime);
        }

        // --- Move uitvoeren ---
        Vector3 finalMove = move * (moveSpeed * slopeSpeedFactor) + velocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    void CheckGrounded()
    {
        float radius = controller.radius;
        Vector3 origin = transform.position + Vector3.up * (controller.center.y - controller.height / 2 + radius);

        grounded = false;

        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            if (hit.collider.CompareTag("Ground") || hit.collider.CompareTag("Start"))
            {
                grounded = true;
                groundHit = hit;
            }
        }

        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, grounded ? Color.green : Color.red);
    }

    public void SetVerticalVelocity(float newVelocity)
    {
        velocity.y = newVelocity;
        lastJumpTime = Time.time;
    }
}
