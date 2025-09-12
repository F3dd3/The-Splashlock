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

    private float yaw;
    private float pitch;
    private Vector3 currentRotation;
    private Vector3 smoothVelocity;

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
    }

    void LateUpdate()
    {
        if (player == null) return;

        bool rotateCamera = false;

        // Camera bewegen hangt af van shift lock of rechtermuisknop
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

        Vector3 targetRotation = new Vector3(pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref smoothVelocity, rotationSmoothTime);
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);

        Vector3 offset = rotation * new Vector3(0, height, -distance);
        transform.position = player.position + offset;

        transform.LookAt(player.position + Vector3.up * 1.5f);
    }
}
