using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsCameraMovement : MonoBehaviour
{
    [Header("Camera Follow")]
    public Transform player;
    public float distance = 5f;
    public float height = 2f;

    [Header("Rotation Settings")]
    [Range(1f, 5f)] public float normalSensitivity = 2f;
    [Range(1f, 5f)] public float shiftedSensitivity = 2f;
    private float yaw, pitch;

    [Header("ShiftLock UI")]
    private RawImage shiftLockInstance;
    [HideInInspector] public bool shiftLockEnabled = false;

    [Header("UI Elements")]
    public Slider normalSensitivitySlider;
    public Slider shiftedSensitivitySlider;
    public TextMeshProUGUI normalSensText;
    public TextMeshProUGUI shiftedSensText;

    private void Start()
    {
        // Als dit script op de camera zit, pak player root
        if (player == null)
            player = transform.root;

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        // Vind automatisch RawImage als child van de camera/player
        shiftLockInstance = GetComponentInChildren<RawImage>(true);
        if (shiftLockInstance != null)
            shiftLockInstance.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Slider setup
        if (normalSensitivitySlider != null)
        {
            normalSensitivitySlider.minValue = 1f;
            normalSensitivitySlider.maxValue = 10f;
            normalSensitivitySlider.value = normalSensitivity;
            normalSensitivitySlider.onValueChanged.AddListener(UpdateNormalSensitivity);
        }

        if (shiftedSensitivitySlider != null)
        {
            shiftedSensitivitySlider.minValue = 1f;
            shiftedSensitivitySlider.maxValue = 10f;
            shiftedSensitivitySlider.value = shiftedSensitivity;
            shiftedSensitivitySlider.onValueChanged.AddListener(UpdateShiftedSensitivity);
        }

        UpdateUIText();
    }

    private void Update()
    {
        // Toggle Shift Lock met linker shift
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftLockEnabled = !shiftLockEnabled;

            Cursor.lockState = shiftLockEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shiftLockEnabled;

            if (shiftLockInstance != null)
                shiftLockInstance.enabled = shiftLockEnabled;
        }

        UpdateUIText();
    }

    private void LateUpdate()
    {
        if (player == null) return;

        if (shiftLockInstance != null)
            shiftLockInstance.enabled = shiftLockEnabled;

        // Kies juiste sensitiviteit
        float currentSensitivity = shiftLockEnabled ? shiftedSensitivity : normalSensitivity;

        // Rotatie alleen als Shift Lock actief of rechter muisknop
        bool rotateCamera = shiftLockEnabled || Input.GetMouseButton(1);
        if (rotateCamera)
        {
            yaw += Input.GetAxis("Mouse X") * currentSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * currentSensitivity;
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

    // ---------------- Slider Events ----------------

    private void UpdateNormalSensitivity(float value)
    {
        normalSensitivity = value;
    }

    private void UpdateShiftedSensitivity(float value)
    {
        shiftedSensitivity = value;
    }

    private void UpdateUIText()
    {
        if (normalSensText != null)
            normalSensText.text = $"Normal Sens: {normalSensitivity:F2}";
        if (shiftedSensText != null)
            shiftedSensText.text = $"Shifted Sens: {shiftedSensitivity:F2}";
    }
}
