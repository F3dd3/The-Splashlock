using UnityEngine;
using Unity.Netcode;

public class LazerDie : NetworkBehaviour
{
    [Header("Respawn Settings")]
    public float respawnHeight = 2f;
    public float spawnProtectionTime = 0.5f;

    private CharacterController controller;
    private float spawnTimestamp;

    private void Awake() => controller = GetComponent<CharacterController>();

    private void Start() => spawnTimestamp = Time.time;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        if (Time.time - spawnTimestamp < spawnProtectionTime) return;

        if (other.CompareTag("Lazer") || other.gameObject.layer == LayerMask.NameToLayer("Lazer"))
        {
            RespawnServerRpc(GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;
        if (Time.time - spawnTimestamp < spawnProtectionTime) return;

        if (collision.collider.CompareTag("Lazer") || collision.gameObject.layer == LayerMask.NameToLayer("Lazer"))
        {
            RespawnServerRpc(GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RespawnServerRpc(ulong networkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkId, out var obj))
        {
            ServerRespawn(obj.gameObject);
        }
    }

    private void ServerRespawn(GameObject playerGO)
    {
        if (playerGO == null) return;

        CheckpointManager manager = FindObjectOfType<CheckpointManager>();
        Vector3 spawnPos = manager != null ? manager.GetSpawnPosition(playerGO) : playerGO.transform.position;
        spawnPos += Vector3.up * respawnHeight;

        // Teleporteer speler server-side
        if (controller != null)
        {
            controller.enabled = false;
            playerGO.transform.position = spawnPos;
            controller.enabled = true;
        }
        else
        {
            playerGO.transform.position = spawnPos;
        }

        var cm = playerGO.GetComponent<CharacterMovement>();
        cm?.SetVerticalVelocity(0f);

        // Sync met owner-client
        ulong ownerId = playerGO.GetComponent<NetworkObject>().OwnerClientId;
        ResetSpawnTimestampClientRpc(spawnPos, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { ownerId } }
        });

        Debug.Log($"[Server] {playerGO.name} respawnt op {spawnPos} door laser");
    }

    [ClientRpc]
    private void ResetSpawnTimestampClientRpc(Vector3 confirmedPos, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;

        if (controller != null)
        {
            controller.enabled = false;
            transform.position = confirmedPos;
            controller.enabled = true;
        }
        else
        {
            transform.position = confirmedPos;
        }

        var cm = GetComponent<CharacterMovement>();
        cm?.SetVerticalVelocity(0f);

        spawnTimestamp = Time.time;
        Debug.Log($"[Client] {gameObject.name} respawn bevestigd op {confirmedPos} door laser");
    }
}
