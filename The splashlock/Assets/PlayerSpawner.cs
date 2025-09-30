using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;

public class PlayerSpawner : NetworkBehaviour
{
    public static PlayerSpawner Instance;

    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints; // [0]=host, [1..]=clients

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Alleen de host/server spawn
            _ = SpawnHostWhenServicesReady();

            // Luister naar client joins
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private async Task SpawnHostWhenServicesReady()
    {
        // Wacht tot Unity Services ready zijn
        while (!UnityServicesInitializer.ServicesInitialized)
            await Task.Yield();

        // Spawn host alleen als server
        SpawnPlayer(NetworkManager.ServerClientId);
    }

    private void OnClientConnected(ulong clientId)
    {
        // Spawn alleen clients, niet de host opnieuw
        if (!UnityServicesInitializer.ServicesInitialized || clientId == NetworkManager.ServerClientId)
            return;

        SpawnPlayer(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned!");
            return;
        }

        int index = GetSpawnIndex(clientId);
        Vector3 spawnPos = spawnPoints[index].position;
        Quaternion spawnRot = spawnPoints[index].rotation;

        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Player prefab must have NetworkObject component!");
            Destroy(playerInstance);
            return;
        }

        // Spawn network object voor de juiste client
        netObj.SpawnAsPlayerObject(clientId, true);

        if (clientId == NetworkManager.ServerClientId)
            Debug.Log("Host spawned (server only).");
        else
            Debug.Log($"Client {clientId} spawned.");
    }

    private int GetSpawnIndex(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId) return 0;

        int joinerIndex = 1;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == clientId) break;
            if (client.ClientId != NetworkManager.ServerClientId) joinerIndex++;
        }

        if (joinerIndex >= spawnPoints.Length)
            joinerIndex = spawnPoints.Length - 1;

        return joinerIndex;
    }
}
