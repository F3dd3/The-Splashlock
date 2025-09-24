using UnityEngine;

public class RotateObject : MonoBehaviour
{
    // Rotatiesnelheid in graden per seconde
    public float rotationSpeed = 90f;

    // As waar omheen gedraaid wordt (bijv Vector3.up voor y-as)
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        // Draai het object elke frame
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}
