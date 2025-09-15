using UnityEngine;
using UnityEngine.UI;

public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    private CharacterController controller;
    private Vector3 velocity;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Shift Lock")]
    public bool shiftLockEnabled = false;

    [Header("Shift Lock UI")]
    public RawImage shiftLockSymbol; // Sleep hier je RawImage naartoe in de Inspector

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f; // afstand onder collider om te checken
    private bool grounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (shiftLockSymbol != null)
            shiftLockSymbol.enabled = false; // standaard uit
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

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * moveZ + right * moveX;

        // Beweeg speler
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Rotatie
        if (shiftLockEnabled)
        {
            // Shift Lock aan: freeze mee met camera
            transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        }
        else
        {
            // Shift Lock uit: draai naar bewegingsrichting met smooth
            if (move.magnitude > 0.05f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
    }

    void HandleGravityAndJump()
    {
        CheckGrounded();

        if (grounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f; // houd speler op de grond

            if (Input.GetButtonDown("Jump"))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
        // SphereCast radius = half van de CharacterController
        float radius = controller.radius;

        // Startpunt: iets boven de onderkant van de collider
        Vector3 origin = transform.position + Vector3.up * (controller.center.y - controller.height / 2 + radius);

        // SphereCast naar beneden
        grounded = Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, groundCheckDistance);

        // Debug: visualiseer de spherecast
        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, grounded ? Color.green : Color.red);
    }
}
