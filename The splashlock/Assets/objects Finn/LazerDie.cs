using UnityEngine;
using Unity.Netcode;

public class LazerDie : NetworkBehaviour
{
    [Header("Respawn Settings")]
    public float respawnHeight = 2f;           // Hoeveel boven spawnpoint spawnen
    public float spawnProtectionTime = 0.5f;   // Tijd na spawn dat speler geen schade kan krijgen

    private CharacterController controller;
    private Vector3 spawnPosition;
    private float spawnTimestamp;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        // Als er geen spawn is ingesteld, fallback naar huidige positie
        if (spawnPosition == Vector3.zero)
            spawnPosition = transform.position;

        spawnTimestamp = Time.time;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Je hoeft hier niets te doen, collisions worden door Unity zelf aangeroepen
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        // Spawn protectie: negeer net na spawn
        if (Time.time - spawnTimestamp < spawnProtectionTime)
            return;

        // Controleer of object een "Lazer" tag heeft of in "Lazer" layer zit
        if (other.CompareTag("Lazer") || other.gameObject.layer == LayerMask.NameToLayer("Lazer"))
        {
            Respawn();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;

        if (Time.time - spawnTimestamp < spawnProtectionTime)
            return;

        // Controleer tag of layer
        if (collision.collider.CompareTag("Lazer") || collision.gameObject.layer == LayerMask.NameToLayer("Lazer"))
        {
            Respawn();
        }
    }

    public void SetSpawnProtection(Vector3 spawnPos)
    {
        spawnPosition = spawnPos + Vector3.up * respawnHeight;
        spawnTimestamp = Time.time;
    }

    private void Respawn()
    {
        if (controller != null)
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            controller.enabled = true;
        }
        else
        {
            transform.position = spawnPosition;
        }

        // Reset verticale snelheid als CharacterMovement aanwezig is
        CharacterMovement cm = GetComponent<CharacterMovement>();
        if (cm != null)
            cm.SetVerticalVelocity(0f);

        spawnTimestamp = Time.time; // spawn protectie opnieuw starten
    }
}
