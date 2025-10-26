using UnityEngine;

public class OptionsCameraMovement : MonoBehaviour
{
    [Header("Camera Follow")]
    public Transform player;
    public float distance = 5f;
    public float height = 2f;

    [Header("Rotation Settings")]
    public float sensitivity = 2f;
    private float yaw, pitch;

    private void Start()
    {
        if (player == null)
            player = transform.root;

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // Eenvoudige rotatie met rechter muisknop
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, -30f, 60f);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0, 0, -distance) + new Vector3(0, height, 0);
        transform.position = player.position + offset;
        transform.LookAt(player.position + Vector3.up * 1.5f);
    }
}
