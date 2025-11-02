using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class Die : NetworkBehaviour
{
    [Header("Respawn Settings")]
    public float respawnHeight = 2f;
    public float checkDistance = 1f;
    public LayerMask waterLayer;
    public float spawnProtectionTime = 0.5f;

    private CharacterController controller;
    private float spawnTimestamp;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        spawnTimestamp = Time.time;
    }

    private void Update()
    {
        // Alleen de server checkt waterdeath
        if (IsServer)
            CheckWaterBelowServer();
    }

    private void CheckWaterBelowServer()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, checkDistance, waterLayer))
        {
            // Water death: negeer spawnProtection, respawn direct
            RespawnPlayer();
        }

        Debug.DrawRay(origin, Vector3.down * checkDistance, Color.blue);
    }

    private void RespawnPlayer()
    {
        CheckpointManager manager = FindObjectOfType<CheckpointManager>();
        Vector3 spawnPos = manager != null ? manager.GetSpawnPosition(gameObject) : transform.position;
        spawnPos += Vector3.up * respawnHeight;

        // Stuur naar owner-client om zichzelf direct te respawnen
        RespawnClientRpc(spawnPos, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { GetComponent<NetworkObject>().OwnerClientId }
            }
        });

        // Update server-side timestamp voor spawnProtection (normale respawns)
        spawnTimestamp = Time.time;
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 spawnPos, ClientRpcParams clientRpcParams = default)
    {
        // Alleen de owner voert dit uit
        if (!IsOwner) return;

        if (controller != null)
        {
            controller.enabled = false;
            transform.position = spawnPos;
            controller.enabled = true;
        }
        else
        {
            transform.position = spawnPos;
        }

        // Reset verticale snelheid
        CharacterMovement cm = GetComponent<CharacterMovement>();
        if (cm != null)
            cm.SetVerticalVelocity(0f);

        // Update spawnTimestamp lokaal (spawnProtection voor normale respawns)
        spawnTimestamp = Time.time;

        Debug.Log($"[Client] Speler '{gameObject.name}' respawnt bij {spawnPos}");
    }
}
