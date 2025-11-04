using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AxeSmash : MonoBehaviour
{
    [Header("Rotation Speed")]
    public Vector3 rotationSpeed = new Vector3(0f, -50f, 0f);

    [Header("Push Settings")]
    public float pushForce = 20f;

    [Header("Swing Settings")]
    public float swingAngle = 45f;      // maximale hoek naar links/rechts
    public float swingSpeed = 2f;       // hoe snel hij zwaait

    private float initialZ;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Start()
    {
        initialZ = transform.eulerAngles.z; // onthoud startrotatie op Z-as
    }

    private void Update()
    {
        // heen-en-weer zwaai in Z-as
        float angle = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
        Vector3 euler = transform.eulerAngles;
        euler.z = initialZ + angle;
        transform.eulerAngles = euler;
    }

    private void ApplyPush(Collider col)
    {
        // Zoek de CharacterMovement component
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
