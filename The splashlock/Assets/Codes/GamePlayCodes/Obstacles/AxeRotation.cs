using UnityEngine;

public class AxeRotation : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.up;  // As waarover je wilt draaien (bijv. Vector3.up, Vector3.right)
    public float rotationSpeed = 90f;          // Graden per seconde
    private bool rotatingForward = true;
    private float rotatedDegrees = 0f;

    void Update()
    {
        float rotationStep = rotationSpeed * Time.deltaTime;

        if (rotatingForward)
        {
            // Rotatie vooruit (180 graden)
            if (rotatedDegrees + rotationStep >= 180f)
            {
                rotationStep = 180f - rotatedDegrees;
                rotatingForward = false;
            }

            transform.Rotate(rotationAxis, rotationStep);
            rotatedDegrees += rotationStep;
        }
        else
        {
            // Rotatie terug (180 graden)
            if (rotatedDegrees - rotationStep <= 0f)
            {
                rotationStep = rotatedDegrees;
                rotatingForward = true;
            }

            transform.Rotate(rotationAxis, -rotationStep);
            rotatedDegrees -= rotationStep;
        }
    }
}
