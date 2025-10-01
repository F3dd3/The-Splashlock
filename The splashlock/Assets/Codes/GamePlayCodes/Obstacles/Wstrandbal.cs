using UnityEngine;

public class PlatformMove : MonoBehaviour
{
    public float dropHeight = 2.5f; // Hoeveel het platform zakt
    public float speed = 0.5f;      // Hoe snel het platform beweegt
    public float checkRadius = 1f;  // Straal van de sphere check

    private Vector3 startPos;
    private Vector3 downPos;

    void Start()
    {
        startPos = transform.position;
        downPos = startPos + Vector3.down * dropHeight;
    }

    void Update()
    {
        // Check of er een speler binnen de sphere zit
        bool playerOnPlatform = false;

        Collider[] hits = Physics.OverlapSphere(transform.position, checkRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                playerOnPlatform = true;
                break;
            }
        }

        // Beweeg het platform
        if (playerOnPlatform)
        {
            transform.position = Vector3.MoveTowards(transform.position, downPos, speed * Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, startPos, speed * Time.deltaTime);
        }
    }

    // Optioneel: laat de sphere zien in de Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}
