using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float slowSpeed = 1f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    public float jumpCooldown = 0.5f;

    [HideInInspector] public bool grounded;

    private Vector3 velocity;
    private float lastJumpTime;
    private CharacterController controller;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Shift Lock")]
    public bool shiftLockEnabled = false;
    public RawImage shiftLockSymbol;

    [Header("Ground Check")]
    public float groundCheckDistance = 2f;
    [HideInInspector] public RaycastHit groundHit;

    [Header("Slope Settings")]
    public float slopeSlideSpeed = 3f;
    public float slopeLimit = 20f;

    [Header("Slow Settings")]
    public float slowCheckDistance = 2f;
    [HideInInspector] public bool onSlowSurface = false;

    [Header("External Forces")]
    public float externalForceDecay = 5f;
    private Vector3 externalForce = Vector3.zero;

    public float VerticalVelocity => velocity.y;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Cursor standaard zichtbaar
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Alleen local player ziet shift lock UI
        if (!IsOwner)
        {
            if (shiftLockSymbol != null)
                shiftLockSymbol.gameObject.SetActive(false);
        }
        else
        {
            if (shiftLockSymbol != null)
                shiftLockSymbol.enabled = false;
        }
    }

    private void Update()
    {
        if (!IsOwner) return; // Alleen local player bestuurt zichzelf en ziet UI

        HandleShiftLock();
        HandleMovement();

        // Externe krachten vervagen
        if (externalForce.magnitude > 0.01f)
            externalForce = Vector3.Lerp(externalForce, Vector3.zero, externalForceDecay * Time.deltaTime);
        else
            externalForce = Vector3.zero;
    }

    private void HandleShiftLock()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftLockEnabled = !shiftLockEnabled;
            Cursor.lockState = shiftLockEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shiftLockEnabled;

            // Alleen voor local player UI
            if (shiftLockSymbol != null)
                shiftLockSymbol.enabled = shiftLockEnabled;
        }
    }

    private void HandleMovement()
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

        grounded = controller.isGrounded;
        CheckGroundedExtra();

        if (Input.GetButton("Jump") && grounded && Time.time - lastJumpTime >= jumpCooldown)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
        }

        if (!grounded)
            velocity.y += gravity * Time.deltaTime;
        else if (velocity.y < 0f)
            velocity.y = -2f;

        float currentSpeed = onSlowSurface ? slowSpeed : moveSpeed;
        Vector3 horizontalMove = moveInput * currentSpeed;

        if (grounded && groundHit.collider != null && groundHit.collider.CompareTag("Helling"))
        {
            float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            if (slopeAngle > slopeLimit)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
                horizontalMove = slideDir * slopeSlideSpeed;
            }
        }

        Vector3 finalMove = horizontalMove + externalForce + new Vector3(0, velocity.y, 0);

        if (shiftLockEnabled)
            transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        else if (moveInput.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720 * Time.deltaTime);
        }

        controller.Move(finalMove * Time.deltaTime);
    }

    private void CheckGroundedExtra()
    {
        float radius = controller.radius;
        Vector3 origin = transform.position + Vector3.up * (controller.center.y - controller.height / 2 + radius);

        onSlowSurface = false;

        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            groundHit = hit;
            if (hit.collider.CompareTag("Slow"))
                onSlowSurface = true;
        }

        if (!onSlowSurface && Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit slowHit, slowCheckDistance))
        {
            if (slowHit.collider.CompareTag("Slow"))
                onSlowSurface = true;
        }
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
