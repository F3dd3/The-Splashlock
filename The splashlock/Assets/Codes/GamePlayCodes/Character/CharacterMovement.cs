using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    public float slowSpeed = 1f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    public float jumpCooldown = 0.5f;

    [Header("Networked Variables")]
    public NetworkVariable<Vector3> velocity = new NetworkVariable<Vector3>(
        new Vector3(0f, -2f, 0f),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [HideInInspector] public bool grounded;
    [HideInInspector] public bool onSlowSurface;

    private float lastJumpTime;
    private CharacterController controller;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Shift Lock")]
    public bool shiftLockEnabled = false;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.3f;
    public float slowCheckDistance = 0.5f;

    [Header("Slope Settings")]
    public float slopeSlideSpeed = 3f;
    public float slopeLimit = 30f;

    [Header("External Forces")]
    public float externalForceDecay = 5f;
    private Vector3 externalForce = Vector3.zero;

    public float VerticalVelocity => velocity.Value.y;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        velocity.Value = new Vector3(0f, -2f, 0f);
    }

    private void Update()
    {
        if (!IsOwner) return;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        bool jump = Input.GetButton("Jump");

        HandleMovement(moveX, moveZ, jump, shiftLockEnabled);
    }

    private void HandleMovement(float moveX, float moveZ, bool jump, bool shiftLock)
    {
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 moveInput = forward * moveZ + right * moveX;
        if (moveInput.magnitude > 1f) moveInput.Normalize();

        grounded = controller.isGrounded;
        CheckGroundedExtra();

        // Jump
        if (jump && grounded && Time.time - lastJumpTime >= jumpCooldown)
        {
            SetVerticalVelocity(Mathf.Sqrt(jumpHeight * -2f * gravity));
            lastJumpTime = Time.time;
        }

        // Gravity
        if (!grounded)
            velocity.Value += new Vector3(0, gravity * Time.deltaTime, 0);
        else if (velocity.Value.y < 0f)
            velocity.Value = new Vector3(velocity.Value.x, -2f, velocity.Value.z);

        float currentSpeed = onSlowSurface ? slowSpeed : moveSpeed;
        Vector3 horizontalMove = moveInput * currentSpeed;

        // Slope sliding
        if (grounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > slopeLimit)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                horizontalMove = slideDir * slopeSlideSpeed;
            }
        }

        Vector3 finalMove = horizontalMove + externalForce + new Vector3(0, velocity.Value.y, 0);

        if (shiftLock)
            transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        else if (moveInput.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, targetAngle, 0), 720 * Time.deltaTime);
        }

        controller.Move(finalMove * Time.deltaTime);

        // External forces decay
        if (externalForce.magnitude > 0.01f)
            externalForce = Vector3.Lerp(externalForce, Vector3.zero, externalForceDecay * Time.deltaTime);
        else
            externalForce = Vector3.zero;
    }

    private void CheckGroundedExtra()
    {
        Vector3 origin = transform.position + Vector3.up * (controller.height / 2 - controller.radius);
        onSlowSurface = false;

        if (Physics.SphereCast(origin, controller.radius, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            if (hit.collider.CompareTag("Slow"))
                onSlowSurface = true;
        }

        if (!onSlowSurface && Physics.SphereCast(origin, controller.radius, Vector3.down, out RaycastHit slowHit, slowCheckDistance))
        {
            if (slowHit.collider.CompareTag("Slow"))
                onSlowSurface = true;
        }
    }

    // --------------------------- Public Methods ---------------------------
    public void SetCamera(Transform camTransform) => cameraTransform = camTransform;

    public void AddExternalForce(Vector3 force) => externalForce += force;

    public void SetVerticalVelocity(float newVelocity)
    {
        Vector3 v = velocity.Value;
        v.y = newVelocity;
        velocity.Value = v;
        lastJumpTime = Time.time;
    }
}
