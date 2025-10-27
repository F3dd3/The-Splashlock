using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AxeSmash : MonoBehaviour
{
    [Header("Rotation Speed")]
    public Vector3 rotationSpeed = new Vector3(0f, -50f, 0f);

    [Header("Push Settings")]
    public float pushForce = 20f;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    private void ApplyPush(Collider col)
    {
        // Zoek de CharacterMovement component (niet meer "Local")
        CharacterMovement player = col.GetComponent<CharacterMovement>();
        if (player != null)
        {
            Vector3 pushDir = (col.transform.position - transform.position);
            pushDir.y = 0f; // alleen horizontaal duwen

            if (pushDir.magnitude < 0.1f)
                pushDir = transform.forward;

            pushDir.Normalize();

            player.AddExternalForce(pushDir * pushForce);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            ApplyPush(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
            ApplyPush(other);
    }
}