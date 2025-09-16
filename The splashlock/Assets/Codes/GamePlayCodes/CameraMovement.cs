using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Camera Settings")]
    public float distance = 5f;         // startafstand
    public float height = 2f;           // vaste hoogte boven speler
    public float sensitivity = 2f;
    public float rotationSmoothTime = 0.1f;

    [Header("Zoom Settings")]
    public float minDistance = 2f;      // minimale zoom
    public float maxDistance = 10f;     // maximale zoom
    public float zoomSpeed = 5f;        // snelheid van scrollen
    public float zoomSmoothTime = 0.1f; // snelheid van smooth zoom

    private float yaw;
    private float pitch;
    private Vector3 currentRotation;
    private Vector3 smoothVelocity;

    private float targetDistance;
    private float currentDistance;
    private float distanceVelocity; // voor SmoothDamp

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

        bool rotateCamera = false;

        // Camera draaien hangt af van shift lock of rechtermuisknop
        if (characterMovement.shiftLockEnabled)
        {
            rotateCamera = true; // altijd draaien
        }
        else if (Input.GetMouseButton(1))
        {
            rotateCamera = true; // alleen bij RMB
        }

        if (rotateCamera)
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

        // Rotatie berekenen
        Vector3 targetRotation = new Vector3(pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref smoothVelocity, rotationSmoothTime);
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);

        // Offset berekenen
        Vector3 offset = rotation * new Vector3(0, 0, -currentDistance); // ZOOM alleen op Z
        offset += new Vector3(0, height, 0); // HOOGTE apart optellen, draait niet mee

        transform.position = player.position + offset;

        // Altijd naar speler kijken
        transform.LookAt(player.position + Vector3.up * 1.5f);
    }
}
