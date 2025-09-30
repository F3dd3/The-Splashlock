using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    public static PlayerSpawner Instance;

    [Header("Spawn Points")]
    public Transform[] spawnPoints; // vul in inspector: host op [0], joiners daarna

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Spawn host meteen bij start
            SpawnPlayer(NetworkManager.ServerClientId);
            NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        int index = GetSpawnIndex(clientId);
        Vector3 spawnPos = spawnPoints[index].position;
        Quaternion spawnRot = spawnPoints[index].rotation;

        GameObject playerPrefab = Resources.Load<GameObject>("Player"); // Zorg dat je prefab in Resources/Player.prefab zit
        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);

        var netObj = playerInstance.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, true);

        Debug.Log($"Speler {clientId} gespawned op {spawnPos}");
    }

    private int GetSpawnIndex(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId) return 0; // host altijd op eerste positie
        return NetworkManager.Singleton.ConnectedClientsList.Count - 1; // joiners daarna
    }

    public Vector3 GetSpawnPosition(int index)
    {
        if (index >= 0 && index < spawnPoints.Length)
            return spawnPoints[index].position;
        return Vector3.zero;
    }
}
