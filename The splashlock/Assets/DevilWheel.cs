using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DevilWheel : MonoBehaviour
{
    [Header("Random Constant Rotation (deg/sec)")]
    public Vector3 rotationSpeedMin = new Vector3(0f, 20f, 0f);
    public Vector3 rotationSpeedMax = new Vector3(0f, 30f, 0f);
    private Vector3 rotationSpeed; // daadwerkelijk toegewezen snelheid

    [Header("Random Oscillation Settings")]
    public Vector3 oscillationAxis = new Vector3(0f, 0f, 1f);
    public float oscillationAmplitudeMin = 10f;
    public float oscillationAmplitudeMax = 20f;
    private float oscillationAmplitude; // daadwerkelijk amplitude

    public float oscillationSpeedMin = 1f;
    public float oscillationSpeedMax = 2f;
    private float oscillationSpeed; // daadwerkelijk oscillatie snelheid

    private Vector3 initialRotation;
    private Vector3 accumulatedRotation;

    void Start()
    {
        initialRotation = transform.eulerAngles;
        accumulatedRotation = Vector3.zero;

        // Random instellen
        rotationSpeed = new Vector3(
            Random.Range(rotationSpeedMin.x, rotationSpeedMax.x),
            Random.Range(rotationSpeedMin.y, rotationSpeedMax.y),
            Random.Range(rotationSpeedMin.z, rotationSpeedMax.z)
        );

        oscillationAmplitude = Random.Range(oscillationAmplitudeMin, oscillationAmplitudeMax);
        oscillationSpeed = Random.Range(oscillationSpeedMin, oscillationSpeedMax);

        // Collider setup
        Collider col = GetComponent<Collider>();
        col.isTrigger = false;
        gameObject.tag = "Helling"; // Voor glijden in CharacterMovement
    }

    void Update()
    {
        // Constante rotatie
        accumulatedRotation += rotationSpeed * Time.deltaTime;

        // Oscillatie
        Vector3 oscillationRotation = oscillationAxis.normalized *
                                      Mathf.Sin(Time.time * oscillationSpeed) *
                                      oscillationAmplitude;

        // Combineer beide
        transform.eulerAngles = initialRotation + accumulatedRotation + oscillationRotation;
    }
}
