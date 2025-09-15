using UnityEngine;
using UnityEngine.UI;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float jumpCooldown = 0.2f; // tijd tussen sprongen als space ingedrukt blijft
    public float rotationSmoothTime = 0.1f; // smooth rotatie tijd

    private CharacterController controller;
    private Vector3 velocity;
    private float lastJumpTime;
    private float rotationVelocity;

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
        HandleMovement();
        HandleGravityAndJump();
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

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Camera basis vectoren
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // Bewegingsvector relatief aan camera
        Vector3 move = forward * moveZ + right * moveX;

        // Normaliseer diagonale beweging
        if (move.magnitude > 1f)
            move.Normalize();

        // Beweeg speler
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Rotatie
        if (shiftLockEnabled)
        {
            // Shift Lock: speler kijkt exact dezelfde kant als camera
            transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        }
        else
        {
            // Shift Lock uit: speler draait smooth naar bewegingsrichting
            if (move.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
                float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0, smoothAngle, 0);
            }
        }
    }

    void HandleGravityAndJump()
    {
        CheckGrounded();

        if (grounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f;

            // Continu springen zolang space ingedrukt is
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

        controller.Move(velocity * Time.deltaTime);
    }

    void CheckGrounded()
    {
        float radius = controller.radius;
        Vector3 origin = transform.position + Vector3.up * (controller.center.y - controller.height / 2 + radius);
        grounded = Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, groundCheckDistance);
        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, grounded ? Color.green : Color.red);
    }
}
