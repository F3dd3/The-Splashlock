using UnityEngine;
using UnityEngine.UI;

public class OptionsCameraMovement : MonoBehaviour
{
    [Header("Camera Follow")]
    public Transform player;
    public float distance = 5f;
    public float height = 2f;

    [Header("Rotation Settings")]
    public float sensitivity = 2f;
    private float yaw, pitch;

    [Header("ShiftLock UI")]
    private RawImage shiftLockInstance; // wordt automatisch gevonden als child van player
    [HideInInspector] public bool shiftLockEnabled = false;

    private void Start()
    {
        if (player == null)
            player = transform.root;

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        // Vind automatisch RawImage als child van player prefab
        if (player != null)
            shiftLockInstance = player.GetComponentInChildren<RawImage>(true);

        if (shiftLockInstance != null)
            shiftLockInstance.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Toggle Shift Lock met linker shift
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftLockEnabled = !shiftLockEnabled;

            // Cursor en UI RawImage instellen
            Cursor.lockState = shiftLockEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shiftLockEnabled;

            if (shiftLockInstance != null)
                shiftLockInstance.enabled = shiftLockEnabled;
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // Shift Lock UI tonen/verbergen (voor zekerheid)
        if (shiftLockInstance != null)
            shiftLockInstance.enabled = shiftLockEnabled;

        // Rotatie alleen als Shift Lock actief of rechter muisknop
        bool rotateCamera = shiftLockEnabled || Input.GetMouseButton(1);
        if (rotateCamera)
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

    public void ToggleShiftLock()
    {
        shiftLockEnabled = !shiftLockEnabled;

        Cursor.lockState = shiftLockEnabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shiftLockEnabled;

        if (shiftLockInstance != null)
            shiftLockInstance.enabled = shiftLockEnabled;
    }
}
