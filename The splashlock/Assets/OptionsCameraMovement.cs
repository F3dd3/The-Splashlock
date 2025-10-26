using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsCameraMovement : MonoBehaviour
{
    [Header("Camera Follow")]
    public Transform player;
    public float distance = 5f;
    public float height = 2f;
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
        if (player == null) player = transform.root;

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        shiftLockInstance = GetComponentInChildren<RawImage>(true);
        if (shiftLockInstance != null) shiftLockInstance.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Slider limits instellen
        if (normalSensitivitySlider != null)
        {
            normalSensitivitySlider.minValue = 0.25f;
            normalSensitivitySlider.maxValue = 25f;
            normalSensitivitySlider.value = RuntimeSettings.NormalSensitivity;
            normalSensitivitySlider.onValueChanged.AddListener(UpdateNormalSensitivity);
        }

        if (shiftedSensitivitySlider != null)
        {
            shiftedSensitivitySlider.minValue = 0.25f;
            shiftedSensitivitySlider.maxValue = 25f;
            shiftedSensitivitySlider.value = RuntimeSettings.ShiftedSensitivity;
            shiftedSensitivitySlider.onValueChanged.AddListener(UpdateShiftedSensitivity);
        }

        UpdateUIText();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            shiftLockEnabled = !shiftLockEnabled;
            Cursor.lockState = shiftLockEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shiftLockEnabled;
            if (shiftLockInstance != null) shiftLockInstance.enabled = shiftLockEnabled;
        }

        UpdateUIText();
    }

    private void LateUpdate()
    {
        if (player == null) return;
        if (shiftLockInstance != null) shiftLockInstance.enabled = shiftLockEnabled;

        float currentSensitivity = shiftLockEnabled ? RuntimeSettings.ShiftedSensitivity : RuntimeSettings.NormalSensitivity;
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

    private void UpdateNormalSensitivity(float value)
    {
        RuntimeSettings.NormalSensitivity = value;
    }

    private void UpdateShiftedSensitivity(float value)
    {
        RuntimeSettings.ShiftedSensitivity = value;
    }

    private void UpdateUIText()
    {
        if (normalSensText != null)
            normalSensText.text = $"Normal Sens: {RuntimeSettings.NormalSensitivity:F2}";
        if (shiftedSensText != null)
            shiftedSensText.text = $"Shifted Sens: {RuntimeSettings.ShiftedSensitivity:F2}";
    }
}
