using UnityEngine;
using Unity.Netcode;

public class Die : NetworkBehaviour
{
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
        if (!IsOwner) return;

        CheckWaterBelow();
    }

    private void CheckWaterBelow()
    {
        if (Time.time - spawnTimestamp < spawnProtectionTime) return;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, checkDistance, waterLayer))
        {
            // Roep server aan om server-side te respawnen
            RespawnServerRpc();
        }

        Debug.DrawRay(origin, Vector3.down * checkDistance, Color.blue);
    }

    // Client vraagt de server om te respawnen voor deze player.
    // RequireOwnership = false zodat de server verzoeken van de owner kan accepteren.
    [ServerRpc(RequireOwnership = false)]
    private void RespawnServerRpc(ServerRpcParams rpcParams = default)
    {
        // Vind de player NetworkObject van de sender
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(senderClientId))
        {
            Debug.LogWarning($"RespawnServerRpc: geen connected client voor id {senderClientId}");
            return;
        }

        var client = NetworkManager.Singleton.ConnectedClients[senderClientId];
        if (client == null || client.PlayerObject == null)
        {
            Debug.LogWarning($"RespawnServerRpc: geen PlayerObject voor client {senderClientId}");
            return;
        }

        GameObject playerGO = client.PlayerObject.gameObject;

        // doe de server-side respawn op het player-go
        ServerRespawn(playerGO);
    }

    // Server-side teleport functie die een player GameObject ontvangt
    private void ServerRespawn(GameObject playerGO)
    {
        if (playerGO == null)
        {
            Debug.LogWarning("ServerRespawn: playerGO is null");
            return;
        }

        CheckpointManager manager = FindObjectOfType<CheckpointManager>();
        Vector3 spawnPos = manager != null ? manager.GetSpawnPosition(playerGO) : playerGO.transform.position;

        spawnPos += Vector3.up * respawnHeight;

        CharacterController playerController = playerGO.GetComponent<CharacterController>();

        if (playerController != null)
        {
            playerController.enabled = false;
            playerGO.transform.position = spawnPos;
            playerController.enabled = true;
        }
        else
        {
            playerGO.transform.position = spawnPos;
        }

        CharacterMovement cm = playerGO.GetComponent<CharacterMovement>();
        if (cm != null)
            cm.SetVerticalVelocity(0f);

        // Belangrijk: update spawnTimestamp alleen op de owner instance (client)
        // We sturen een ClientRpc naar de owner om spawnTimestamp bij te werken
        ulong ownerId = playerGO.GetComponent<NetworkObject>().OwnerClientId;
        ResetSpawnTimestampClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { ownerId } }
        });

        Debug.Log($"[Server] Speler '{playerGO.name}' respawnt bij {spawnPos}");
    }

    // Instructie naar de owner-client om spawnTimestamp te resetten (zodat spawn-protection blijft werken client-side)
    [ClientRpc]
    private void ResetSpawnTimestampClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner)
        {
            // We willen dit alleen lokaal op de owner uitvoeren; maar ClientRpc is gericht aan owner, dus fine.
        }
        spawnTimestamp = Time.time;
    }

    // Voor lokale (non-server) Respawn fallback - niet gebruikt voor multiplayer flow maar behouden voor safety
    public void Respawn()
    {
        CheckpointManager manager = FindObjectOfType<CheckpointManager>();
        Vector3 spawnPos = manager != null ? manager.GetSpawnPosition(gameObject) : transform.position;

        spawnPos += Vector3.up * respawnHeight;

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

        CharacterMovement cm = GetComponent<CharacterMovement>();
        if (cm != null)
            cm.SetVerticalVelocity(0f);

        spawnTimestamp = Time.time;
        Debug.Log($"Speler '{gameObject.name}' respawnt bij '{spawnPos}' (lokale Respawn)");
    }
}
