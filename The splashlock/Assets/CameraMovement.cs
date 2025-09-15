using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Camera Settings")]
    public float distance = 5f;
    public float height = 2f;
    public float sensitivity = 2f;
    public float rotationSmoothTime = 0.1f;

    [Header("Zoom Settings")]
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float zoomSpeed = 5f;
    public float zoomSmoothTime = 0.1f;

    private float yaw;
    private float pitch;
    private Vector3 currentRotation;
    private Vector3 smoothVelocity;

    private float targetDistance;
    private float currentDistance;
    private float distanceVelocity;

    private CharacterMovement characterMovement;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("⚠️ Player transform niet toegewezen!");
            return;
        }

        characterMovement = player.GetComponent<CharacterMovement>();

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        currentDistance = distance;
        targetDistance = distance;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Camera draaien alleen bij rechtermuisknop
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, -30f, 60f);
        }

        // Zoom via muiswiel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        // Smooth zoom
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);

        // Bereken rotatie
        Vector3 targetRotation = new Vector3(pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref smoothVelocity, rotationSmoothTime);
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);

        // Camera offset zonder shift lock
        Vector3 offset = rotation * new Vector3(0, height, -currentDistance);

        transform.position = player.position + offset;
        transform.LookAt(player.position + Vector3.up * 1.5f);
    }
}
