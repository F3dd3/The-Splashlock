using UnityEngine;

public class Stick2 : MonoBehaviour
{
    [Header("Rotation Speed")]
    public Vector3 rotationSpeed = new Vector3(0f, -50f, 0f);
    // X, Y, Z graden per seconde

    void Update()
    {
        // Draai object per frame
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
