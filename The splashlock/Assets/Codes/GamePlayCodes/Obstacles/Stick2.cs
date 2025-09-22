using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Stick2 : MonoBehaviour
{
    [Header("Rotation Speed")]
    public Vector3 rotationSpeed = new Vector3(0f, -50f, 0f);

    [Header("Push Settings")]
    public float pushForce = 20f;  // kracht waarmee speler wordt weggeduwd

    private void Awake()
    {
        // Collider als trigger instellen
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Update()
    {
        // Draai object per frame
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        CharacterMovement player = other.GetComponent<CharacterMovement>();
        if (player != null)
        {
            Vector3 pushDir = (other.transform.position - transform.position).normalized;
            pushDir.y = 0f;
            player.AddExternalForce(pushDir * pushForce);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        CharacterMovement player = other.GetComponent<CharacterMovement>();
        if (player != null)
        {
            Vector3 pushDir = (other.transform.position - transform.position).normalized;
            pushDir.y = 0f;
            player.AddExternalForce(pushDir * pushForce * Time.deltaTime);
        }
    }
}
