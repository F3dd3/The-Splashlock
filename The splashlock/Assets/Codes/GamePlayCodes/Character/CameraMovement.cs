using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class CameraMovement : NetworkBehaviour
{
    [Header("Camera Follow")]
    public Transform player;
    public float distance = 5f;
    public float height = 2f;

    [Header("Rotation Settings")]
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

    [Header("Options Canvas")]
    public Canvas optionsCanvas;            // Canvas op player
    public Canvas mainCanvas;               // Andere canvas die verdwijnt bij options
    public Button openOptionsButton;        // Knop in scene
    public Button backButton;               // Knop in options canvas
    public Slider normalSensitivitySlider;
    public Slider shiftedSensitivitySlider;
    public TextMeshProUGUI normalSensText;
    public TextMeshProUGUI shiftedSensText;

    private CharacterMovement characterMovement;

    private void Awake()
    {
        // ❗ Zet default instellingen bij het opstarten van het spel
        if (!RuntimeSettings.SessionStarted)
        {
            RuntimeSettings.NormalSensitivity = 2f;
            RuntimeSettings.ShiftedSensitivity = 2f;
            RuntimeSettings.SessionStarted = true;
        }
    }

    private void Start()
    {
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        if (player == null) player = transform.root;

        characterMovement = player.GetComponent<CharacterMovement>();
        if (characterMovement != null)
            characterMovement.SetCamera(transform);

        if (shiftLockPrefab != null)
        {
            shiftLockInstance = Instantiate(shiftLockPrefab, transform);
            shiftLockInstance.enabled = false;
        }

        // Canvas startstatus
        if (optionsCanvas != null) optionsCanvas.enabled = false;
        if (mainCanvas != null) mainCanvas.enabled = true;

        // Open Options Button
        if (openOptionsButton != null)
        {
            openOptionsButton.onClick.AddListener(() =>
            {
                if (optionsCanvas != null && mainCanvas != null)
                {
                    optionsCanvas.enabled = true;
                    mainCanvas.enabled = false;
                    UpdateSlidersAndText();
                }
            });
        }

        // Back Button in Options
        if (backButton != null)
        {
            backButton.onClick.AddListener(() =>
            {
                if (optionsCanvas != null && mainCanvas != null)
                {
                    optionsCanvas.enabled = false;
                    mainCanvas.enabled = true;
                }
            });
        }

        // Slider setup
        normalSensitivitySlider.minValue = 0.25f;
        normalSensitivitySlider.maxValue = 25f;
        shiftedSensitivitySlider.minValue = 0.25f;
        shiftedSensitivitySlider.maxValue = 25f;

        normalSensitivitySlider.onValueChanged.AddListener(UpdateNormalSensitivity);
        shiftedSensitivitySlider.onValueChanged.AddListener(UpdateShiftedSensitivity);

        // Gebruik instellingen uit RuntimeSettings (sessie) i.p.v. PlayerPrefs
        UpdateSlidersAndText();

        currentDistance = targetDistance = distance;
        currentRotation = transform.eulerAngles;
        yaw = currentRotation.y;
        pitch = currentRotation.x;
    }

    private void UpdateSlidersAndText()
    {
        if (normalSensitivitySlider != null)
            normalSensitivitySlider.value = RuntimeSettings.NormalSensitivity;
        if (shiftedSensitivitySlider != null)
            shiftedSensitivitySlider.value = RuntimeSettings.ShiftedSensitivity;

        if (normalSensText != null)
            normalSensText.text = $"Normal Sens: {RuntimeSettings.NormalSensitivity:F2}";
        if (shiftedSensText != null)
            shiftedSensText.text = $"Shifted Sens: {RuntimeSettings.ShiftedSensitivity:F2}";
    }

    private void UpdateNormalSensitivity(float value)
    {
        RuntimeSettings.NormalSensitivity = value;
        if (normalSensText != null)
            normalSensText.text = $"Normal Sens: {value:F2}";
    }

    private void UpdateShiftedSensitivity(float value)
    {
        RuntimeSettings.ShiftedSensitivity = value;
        if (shiftedSensText != null)
            shiftedSensText.text = $"Shifted Sens: {value:F2}";
    }

    private void LateUpdate()
    {
        if (!IsOwner || player == null) return;

        if (shiftLockInstance != null && characterMovement != null)
            shiftLockInstance.enabled = !PauseMenu.IsPaused && characterMovement.shiftLockEnabled;

        float currentSensitivity = (characterMovement != null && characterMovement.shiftLockEnabled)
                                    ? RuntimeSettings.ShiftedSensitivity
                                    : RuntimeSettings.NormalSensitivity;

        bool rotateCamera = (characterMovement != null && characterMovement.shiftLockEnabled) || Input.GetMouseButton(1);
        if (rotateCamera)
        {
            yaw += Input.GetAxis("Mouse X") * currentSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * currentSensitivity;
            pitch = Mathf.Clamp(pitch, -30f, 60f);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);

        Vector3 targetRotation = new Vector3(pitch, yaw);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref smoothVelocity, rotationSmoothTime);
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);

        Vector3 offset = rotation * new Vector3(0, 0, -currentDistance) + new Vector3(0, height, 0);
        transform.position = player.position + offset;
        transform.LookAt(player.position + Vector3.up * 1.5f);
    }
}

// Voeg dit toe als aparte class (zelfde als OptionsCameraMovement)

