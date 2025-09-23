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

    private void ApplyPush(Collider col)
    {
        CharacterMovement player = col.GetComponent<CharacterMovement>();
        if (player != null)
        {
            Vector3 pushDir = (col.transform.position - transform.position);
            pushDir.y = 0f; // alleen horizontale push
            if (pushDir.magnitude < 0.1f)
            {
                // fallback richting als speler exact in het midden staat
                pushDir = transform.forward;
            }
            pushDir.Normalize();

            // voeg externe kracht toe
            player.AddExternalForce(pushDir * pushForce);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ApplyPush(other);
    }

    private void OnTriggerStay(Collider other)
    {
        ApplyPush(other);
    }
}
