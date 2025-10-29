using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class CheckpointManager : NetworkBehaviour
{
    [Header("Checkpoints Setup")]
    [Tooltip("Het eerste platform waar de speler begint.")]
    public Checkpoint startPlatform;

    [Tooltip("Alle checkpoints in volgorde")]
    public List<Checkpoint> checkpoints = new List<Checkpoint>();

    // Houdt per speler hun spawnpunt bij (gebruik OwnerClientId voor Netcode)
    private Dictionary<ulong, Transform> playerSpawnPoints = new Dictionary<ulong, Transform>();

    // Wordt aangeroepen (server-side) om een checkpoint te activeren voor een speler
    public void ActivateCheckpoint(Checkpoint checkpoint, GameObject player)
    {
        if (checkpoint == null || player == null) return;
        if (checkpoint.spawnPoint == null)
        {
            Debug.LogWarning($"Checkpoint '{checkpoint.name}' heeft geen spawnPoint!");
            return;
        }

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogWarning($"ActivateCheckpoint: player '{player.name}' heeft geen NetworkObject component!");
            return;
        }

        if (!IsServer)
        {
            Debug.LogWarning($"ActivateCheckpoint werd op een client aangeroepen voor player '{player.name}'. Dit moet op de server gebeuren.");
            return;
        }

        playerSpawnPoints[netObj.OwnerClientId] = checkpoint.spawnPoint;
        Debug.Log($"[Server] Speler '{player.name}' ({netObj.OwnerClientId}) activeerde checkpoint '{checkpoint.name}' op spawn {checkpoint.spawnPoint.position}");
    }

    // Wordt gebruikt door Die.cs om de spawnpositie te vinden
    public Vector3 GetSpawnPosition(GameObject player)
    {
        if (player == null)
        {
            Debug.LogWarning("GetSpawnPosition: player is null");
            return Vector3.zero;
        }

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null && playerSpawnPoints.TryGetValue(netObj.OwnerClientId, out Transform spawn))
        {
            Debug.Log($"GetSpawnPosition: gevonden spawn voor player {player.name} -> {spawn.position}");
            return spawn.position;
        }

        // fallback: startPlatform
        if (startPlatform != null && startPlatform.spawnPoint != null)
        {
            Debug.Log($"GetSpawnPosition: fallback naar startPlatform {startPlatform.spawnPoint.position} voor player {player.name}");
            return startPlatform.spawnPoint.position;
        }

        Debug.LogWarning($"GetSpawnPosition: geen spawn gevonden voor player {player.name}, fallback naar huidige positie");
        return player.transform.position;
    }
}
