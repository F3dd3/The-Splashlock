using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CameraMovement : NetworkBehaviour
{
    [Header("Camera Follow")]
    public Transform player;
    public float distance = 5f;
    public float height = 2f;

    [Header("Rotation Settings")]
    public float sensitivity = 2f;
    public float rotationSmoothTime = 0.1f;
    private float yaw, pitch;
    private Vector3 currentRotation;
    private Vector3 smoothVelocity;

    [Header("Zoom Settings")]
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float zoomSpeed = 5f;
    public float zoomSmoothTime = 0.1f;
    private float targetDistance, currentDistance, distanceVelocity;

    [Header("ShiftLock UI")]
    public RawImage shiftLockPrefab;
    private RawImage shiftLockInstance;

    private CharacterMovement characterMovement;

    // --------------------------- Initialization ---------------------------
    private void Start()
    {
        // Alleen owner mag camera gebruiken
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        if (player == null)
            player = transform.root;

        // Link CharacterMovement
        characterMovement = player.GetComponent<CharacterMovement>();
        if (characterMovement != null)
            characterMovement.SetCamera(transform); // setter gebruiken!

        // ShiftLock UI instantiëren
        if (shiftLockPrefab != null)
        {
            shiftLockInstance = Instantiate(shiftLockPrefab, transform);
            shiftLockInstance.enabled = false;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        currentDistance = targetDistance = distance;
        currentRotation = angles;
    }

    // --------------------------- Camera Update ---------------------------
    private void LateUpdate()
    {
        if (!IsOwner || player == null) return;

        bool rotateCamera = (characterMovement != null && characterMovement.shiftLockEnabled) || Input.GetMouseButton(1);

        // ShiftLock UI tonen indien actief
        if (shiftLockInstance != null && characterMovement != null)
            shiftLockInstance.enabled = characterMovement.shiftLockEnabled;

        // ------------------- Rotatie -------------------
        if (rotateCamera)
        {
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, -30f, 60f);
        }

        // ------------------- Zoom -------------------
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);

        // ------------------- Smooth Rotation -------------------
        Vector3 targetRotation = new Vector3(pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref smoothVelocity, rotationSmoothTime);
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);

        // ------------------- Camera Position -------------------
        Vector3 offset = rotation * new Vector3(0, 0, -currentDistance) + new Vector3(0, height, 0);
        transform.position = player.position + offset;

        // Kijk altijd naar hoofd van player (1.5 unit omhoog)
        transform.LookAt(player.position + Vector3.up * 1.5f);
    }

    // --------------------------- Helper voor andere scripts ---------------------------
    public void SetOwnerCamera(bool active)
    {
        if (shiftLockInstance != null)
            shiftLockInstance.enabled = active;

        gameObject.SetActive(active);
    }
}
