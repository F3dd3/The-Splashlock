using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    public float slowSpeed = 1f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    public float jumpCooldown = 0.5f;

    [Header("Smash Settings")]
    public float smashForce = 8f;
    public float smashCooldown = 1f;
    private float lastSmashTime;

    [Header("Network Variables")]
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
    public RawImage shiftLockImage;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.3f;
    public float slowCheckDistance = 0.5f;

    [Header("Slope Settings")]
    public float slopeSlideSpeed = 3f;
    public float slopeLimit = 30f;

    [Header("External Forces")]
    public float externalForceDecay = 5f;
    private Vector3 externalForce = Vector3.zero;

    private float spawnTime;
    public float VerticalVelocity => velocity.Value.y;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        velocity.Value = new Vector3(0f, -2f, 0f);

        if (shiftLockImage != null)
            shiftLockImage.enabled = false;
    }

    private void Start()
    {
        spawnTime = Time.time;
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleShiftLock();

        if (!PauseMenu.IsPaused)
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");
            bool jump = Input.GetButton("Jump");

            HandleMovement(moveX, moveZ, jump, shiftLockEnabled);
        }
    }

    private void LateUpdate()
    {
        // Geen cursor logica meer hier; alles via PauseMenu
    }

    private void HandleShiftLock()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !PauseMenu.IsPaused)
        {
            shiftLockEnabled = !shiftLockEnabled;

            Cursor.lockState = shiftLockEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shiftLockEnabled;

            if (shiftLockImage != null)
                shiftLockImage.enabled = shiftLockEnabled && !PauseMenu.IsPaused;
        }
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

        CheckSlowSurfaces();
        CheckSmashHit();

        if (jump && grounded && Time.time - lastJumpTime >= jumpCooldown)
        {
            SetVerticalVelocity(Mathf.Sqrt(jumpHeight * -2f * gravity));
            lastJumpTime = Time.time;
        }

        if (!grounded)
            velocity.Value += new Vector3(0, gravity * Time.deltaTime, 0);
        else if (velocity.Value.y < 0f)
            velocity.Value = new Vector3(velocity.Value.x, -2f, velocity.Value.z);

        float currentSpeed = onSlowSurface ? slowSpeed : moveSpeed;
        Vector3 horizontalMove = moveInput * currentSpeed;

        if (grounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 1f))
        {
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            if (slopeAngle > slopeLimit)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal).normalized;
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

        if (externalForce.magnitude > 0.01f)
            externalForce = Vector3.Lerp(externalForce, Vector3.zero, externalForceDecay * Time.deltaTime);
        else
            externalForce = Vector3.zero;
    }

    private void CheckSlowSurfaces()
    {
        onSlowSurface = false;
        if (Time.time - spawnTime < 0.1f) return;

        Vector3 origin = transform.position + Vector3.up * (controller.height / 2 - controller.radius);
        if (Physics.SphereCast(origin, controller.radius, Vector3.down, out RaycastHit hit, slowCheckDistance))
        {
            if (hit.collider.CompareTag("Slow") || hit.collider.CompareTag("Smash"))
                onSlowSurface = true;
        }
    }

    private void CheckSmashHit()
    {
        if (Time.time - spawnTime < 0.1f) return;

        Vector3 origin = transform.position + Vector3.up * (controller.height / 2);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 0.6f))
        {
            if (hit.collider.CompareTag("Smash") && Time.time - lastSmashTime >= smashCooldown)
            {
                lastSmashTime = Time.time;
                Vector3 dir = (transform.position - hit.point).normalized;
                dir.y = 0.5f;
                AddExternalForce(dir * smashForce);
            }
        }
    }

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
