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
            _ = SpawnHostWhenServicesReady();
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
        while (!UnityServicesInitializer.ServicesInitialized)
            await Task.Yield();

        SpawnPlayer(NetworkManager.ServerClientId);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!UnityServicesInitializer.ServicesInitialized) return;
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

        // Spawn network object
        netObj.SpawnAsPlayerObject(clientId, true);

        // Zet naam server-side zodat alle clients het krijgen
        string playerName = clientId == NetworkManager.ServerClientId ? "Host" : $"Speler {index}";
        var nameTag = playerInstance.GetComponent<PlayerNameTag>();
        if (nameTag != null)
        {
            nameTag.playerName.Value = playerName;
        }

        Debug.Log($"{playerName} spawned at {spawnPos}");
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
