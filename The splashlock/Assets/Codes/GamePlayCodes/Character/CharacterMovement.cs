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

    [Header("Debug")]
    public Vector3 velocity;

    [HideInInspector] public bool grounded;
    [HideInInspector] public RaycastHit groundHit;
    [HideInInspector] public bool onSlowSurface = false;

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

    public NetworkVariable<Vector3> NetworkVelocity = new NetworkVariable<Vector3>(
        new Vector3(0, -2f, 0),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public float VerticalVelocity => velocity.y;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // owner velocity init
            velocity = new Vector3(0, -2f, 0);
            NetworkVelocity.Value = velocity;
        }
        else
        {
            // niet-owner: velocity alvast op -2 zodat player niet door de grond valt
            velocity = new Vector3(0, -2f, 0);
        }

        // cameraTransform fix
        if (IsOwner && cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        grounded = controller.isGrounded;
        CheckGroundedExtra();

        if (IsOwner)
        {
            HandleShiftLock();
            HandleMovement();

            // update NetworkVariable
            NetworkVelocity.Value = velocity;

            // Externe krachten
            if (externalForce.magnitude > 0.01f)
                externalForce = Vector3.Lerp(externalForce, Vector3.zero, externalForceDecay * Time.deltaTime);
            else
                externalForce = Vector3.zero;
        }
        else
        {
            // gebruik NetworkVariable voor niet-owner
            velocity = NetworkVelocity.Value;

            // always grounded fix
            if (grounded && velocity.y < 0f)
                velocity.y = -2f;
        }
    }

    private void HandleShiftLock()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftLockEnabled = !shiftLockEnabled;
            Cursor.lockState = shiftLockEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shiftLockEnabled;
        }
    }

    private void HandleMovement()
    {
        if (!IsOwner) return;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();

        Vector3 moveInput = forward * moveZ + right * moveX;
        if (moveInput.magnitude > 1f) moveInput.Normalize();

        // jump
        if (Input.GetButton("Jump") && grounded && Time.time - lastJumpTime >= jumpCooldown)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
        }

        // gravity
        if (!grounded) velocity.y += gravity * Time.deltaTime;
        else if (velocity.y < 0f) velocity.y = -2f;

        float currentSpeed = onSlowSurface ? slowSpeed : moveSpeed;
        Vector3 horizontalMove = moveInput * currentSpeed;

        // slope slide
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

        // rotation
        if (shiftLockEnabled)
            transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        else if (moveInput.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(moveInput.x, moveInput.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, targetAngle, 0), 720 * Time.deltaTime);
        }

        controller.Move(finalMove * Time.deltaTime);
    }

    private void CheckGroundedExtra()
    {
        Vector3 origin = transform.position + Vector3.up * (controller.height / 2 - controller.radius);

        onSlowSurface = false;

        if (Physics.SphereCast(origin, controller.radius, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            groundHit = hit;
            if (hit.collider.CompareTag("Slow"))
                onSlowSurface = true;
        }

        if (!onSlowSurface && Physics.SphereCast(origin, controller.radius, Vector3.down, out RaycastHit slowHit, slowCheckDistance))
        {
            if (slowHit.collider.CompareTag("Slow"))
                onSlowSurface = true;
        }
    }

    public void SetCamera(Transform camTransform) => cameraTransform = camTransform;
    public void AddExternalForce(Vector3 force) => externalForce += force;
    public void SetVerticalVelocity(float newVelocity)
    {
        velocity.y = newVelocity;
        lastJumpTime = Time.time;

        if (IsOwner)
            NetworkVelocity.Value = velocity;
    }
}
