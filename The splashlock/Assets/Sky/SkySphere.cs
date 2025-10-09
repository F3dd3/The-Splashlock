using UnityEngine;

public class SkySphere : MonoBehaviour
{
    [Header("Rotatie Instellingen")]
    [Tooltip("Snelheid van de rotatie in graden per seconde.")]
    [SerializeField] private float rotationSpeed = 10f;

    void Update()
    {
        // Draai het object rond de Y-as van de wereld
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
    }
}
