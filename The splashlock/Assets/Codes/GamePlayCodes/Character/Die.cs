using UnityEngine;
using Unity.Netcode;

public class Die : NetworkBehaviour
{
    [Header("Respawn Settings")]
    public float respawnHeight = 2f;           // Hoeveel boven spawnpoint spawnen
    public float checkDistance = 1f;           // Hoe ver de raycast onder de speler checkt
    public LayerMask waterLayer;               // Layer voor water (zorg dat je Water objecten hierin hebt)
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

        CheckWaterBelow();
    }

    private void CheckWaterBelow()
    {
        // Spawn protectie: negeer checks net na spawn
        if (Time.time - spawnTimestamp < spawnProtectionTime)
            return;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, checkDistance, waterLayer))
        {
            if (hit.collider.CompareTag("Water"))
                Respawn();
        }

        // Debug zichtbaar maken in Scene
        Debug.DrawRay(origin, Vector3.down * checkDistance, Color.blue);
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

        // Reset vertical velocity als je CharacterMovement hebt
        CharacterMovement cm = GetComponent<CharacterMovement>();
        if (cm != null)
            cm.SetVerticalVelocity(0f);

        spawnTimestamp = Time.time; // heractiveer spawn protection
    }
}
