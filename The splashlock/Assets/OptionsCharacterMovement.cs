using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class OptionsCharacterMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float gravity = -9.81f;
    public float jumpHeight = 1f;
    public float jumpCooldown = 0.5f;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Shift Lock")]
    [HideInInspector] public bool shiftLockEnabled = false;
    private RawImage shiftLockImage; // wordt automatisch gevonden als child van player

    private CharacterController controller;
    private Vector3 velocity;
    private float lastJumpTime;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        velocity = new Vector3(0f, -2f, 0f);

        // Vind automatisch de RawImage als child
        shiftLockImage = GetComponentInChildren<RawImage>(true);
        if (shiftLockImage != null)
            shiftLockImage.enabled = false;
    }

    private void Update()
    {
        HandleShiftLock();
        HandleMovement();
    }

    private void HandleShiftLock()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftLockEnabled = !shiftLockEnabled;

            // Cursor verbergen/toon en UI RawImage
            Cursor.lockState = shiftLockEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shiftLockEnabled;

            if (shiftLockImage != null)
                shiftLockImage.enabled = shiftLockEnabled;
        }
    }

    private void HandleMovement()
    {
        // Input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        bool jump = Input.GetButton("Jump");

        // Direction
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();
        Vector3 moveInput = forward * moveZ + right * moveX;
        if (moveInput.magnitude > 1f) moveInput.Normalize();

        // Grounded check
        bool grounded = controller.isGrounded;

        // Jump
        if (jump && grounded && Time.time - lastJumpTime >= jumpCooldown)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
        }

        // Gravity
        if (!grounded)
            velocity.y += gravity * Time.deltaTime;
        else if (velocity.y < 0f)
            velocity.y = -2f;

        // Movement
        Vector3 horizontalMove = moveInput * moveSpeed;
        Vector3 finalMove = horizontalMove + new Vector3(0, velocity.y, 0);

        // Rotate player
        if (shiftLockEnabled)
        {
            // Draait mee met camera bij shift lock
            transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        }
        else if (moveInput.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, targetAngle, 0), 720 * Time.deltaTime);
        }

        controller.Move(finalMove * Time.deltaTime);
    }

    public bool grounded => controller.isGrounded;
    public float VerticalVelocity => velocity.y;
}
