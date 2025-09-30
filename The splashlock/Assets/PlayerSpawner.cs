using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    public static PlayerSpawner Instance;

    [Header("Spawn Points")]
    public Transform[] spawnPoints; // [0] = host, [1..N] = joiners

    [Header("Player Prefab")]
    public GameObject playerPrefab; // sleep hier je Player prefab in inspector

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Spawn host player
            SpawnPlayer(NetworkManager.ServerClientId);

            // Callback voor joiners
            NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not assigned in PlayerSpawner!");
            return;
        }

        int index = GetSpawnIndex(clientId);
        Vector3 spawnPos = spawnPoints[index].position;
        Quaternion spawnRot = spawnPoints[index].rotation;

        // Instantiate prefab
        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);

        // NetworkObject component
        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId, true);
            Debug.Log($"Player {clientId} spawned at {spawnPos}");

            // Notify broadcaster
            if (PlayerBroadcaster.Instance != null)
                PlayerBroadcaster.Instance.OnPlayerJoined(clientId, index);
        }
        else
        {
            Debug.LogError("Player prefab must have a NetworkObject component!");
            Destroy(playerInstance);
        }
    }

    private int GetSpawnIndex(ulong clientId)
    {
        // Host altijd index 0
        if (clientId == NetworkManager.ServerClientId) return 0;

        // Joiners krijgen de volgende posities
        int index = NetworkManager.Singleton.ConnectedClientsList.Count;
        if (index >= spawnPoints.Length)
        {
            Debug.LogWarning("Not enough spawn points! Using last spawn point.");
            index = spawnPoints.Length - 1;
        }
        return index;
    }
}
