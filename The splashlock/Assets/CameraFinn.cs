using UnityEngine;
using UnityEngine.UI;

public class CameraMovement_Local : MonoBehaviour
{
    public Transform player;
    public float distance = 5f;
    public float height = 2f;
    public float sensitivity = 2f;
    public float rotationSmoothTime = 0.1f;

    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float zoomSpeed = 5f;
    public float zoomSmoothTime = 0.1f;

    public RawImage shiftLockPrefab;
    private RawImage shiftLockInstance;

    private float yaw, pitch;
    private Vector3 currentRotation, smoothVelocity;
    private float targetDistance, currentDistance, distanceVelocity;
    private CharacterMovement_Local characterMovement;

    private void Start()
    {
        if (player == null)
            player = transform.root;

        characterMovement = player.GetComponent<CharacterMovement_Local>();

        if (characterMovement != null)
            characterMovement.cameraTransform = transform;

        if (shiftLockPrefab != null)
        {
            shiftLockInstance = Instantiate(shiftLockPrefab, transform);
            shiftLockInstance.enabled = false;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        currentDistance = targetDistance = distance;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        bool rotateCamera = (characterMovement != null && characterMovement.shiftLockEnabled) || Input.GetMouseButton(1);

        if (shiftLockInstance != null)
            shiftLockInstance.enabled = characterMovement != null && characterMovement.shiftLockEnabled;

        if (rotateCamera)
        {
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, -30f, 60f);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);

        Vector3 targetRotation = new Vector3(pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref smoothVelocity, rotationSmoothTime);
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);

        Vector3 offset = rotation * new Vector3(0, 0, -currentDistance) + new Vector3(0, height, 0);
        transform.position = player.position + offset;
        transform.LookAt(player.position + Vector3.up * 1.5f);
    }
}
