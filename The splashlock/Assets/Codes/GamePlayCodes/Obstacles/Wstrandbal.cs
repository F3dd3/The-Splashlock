using UnityEngine;

public class PlatformMove : MonoBehaviour
{
    public float dropHeight = 2.5f; // Hoeveel het platform zakt
    public float speed = 0.5f;      // Hoe snel het platform beweegt

    private Vector3 startPos;
    private Vector3 downPos;
    private bool playerOnPlatform = false;

    void Start()
    {
        startPos = transform.position;
        downPos = startPos + Vector3.down * dropHeight;
    }

    void Update()
    {
        if (playerOnPlatform)
        {
            // Zak naar beneden
            transform.position = Vector3.MoveTowards(transform.position, downPos, speed * Time.deltaTime);
        }
        else
        {
            // Ga weer omhoog
            transform.position = Vector3.MoveTowards(transform.position, startPos, speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnPlatform = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnPlatform = false;
        }
    }
}
