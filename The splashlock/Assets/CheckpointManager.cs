using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class CheckpointManager : NetworkBehaviour
{
    [Header("Start Platform")]
    public Checkpoint startPlatform;

    private Dictionary<ulong, Transform> playerSpawnPoints = new Dictionary<ulong, Transform>();

    public void ActivateCheckpoint(Checkpoint checkpoint, GameObject player)
    {
        if (!IsServer) return;
        if (checkpoint == null || checkpoint.spawnPoint == null) return;
        if (player == null) return;

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj == null) return;

        playerSpawnPoints[netObj.OwnerClientId] = checkpoint.spawnPoint;
        Debug.Log($"[Server] Player '{player.name}' activeerde checkpoint '{checkpoint.name}' (ID {checkpoint.checkpointId})");
    }

    public Vector3 GetSpawnPosition(GameObject player)
    {
        if (player == null) return Vector3.zero;

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null && playerSpawnPoints.TryGetValue(netObj.OwnerClientId, out Transform spawn))
        {
            return spawn.position;
        }

        // fallback naar startPlatform
        if (startPlatform != null && startPlatform.spawnPoint != null)
            return startPlatform.spawnPoint.position;

        return player.transform.position;
    }
}
