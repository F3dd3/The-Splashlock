using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GamePlayerSpawner : NetworkBehaviour
{
    public GameObject gamePlayerPrefab;
    public Transform[] spawnPoints;

    private int nextSpawnIndex = 0;

    private void Awake()
    {
        // Zorg dat we een callback krijgen wanneer scene geladen is
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayerForClient;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsServer) return;

        // Server spawnt alle clients die al connected zijn
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            SpawnPlayerForClient(client.ClientId);
        }

        // Subscribe toekomstige clients
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayerForClient;
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        if (gamePlayerPrefab == null)
        {
            Debug.LogError("⚠️ GamePlayer prefab niet ingesteld!");
            return;
        }

        Transform spawn = GetNextSpawnPoint();
        GameObject player = Instantiate(gamePlayerPrefab, spawn.position, spawn.rotation);

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.SpawnAsPlayerObject(clientId, true);

        // Zorg dat alleen de owner camera activeert
        CameraMovement cam = player.GetComponentInChildren<CameraMovement>();
        if (cam != null)
            cam.gameObject.SetActive(clientId == NetworkManager.Singleton.LocalClientId);
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints.Length == 0)
        {
            GameObject dummy = new GameObject("SpawnPoint");
            return dummy.transform;
        }

        Transform t = spawnPoints[nextSpawnIndex % spawnPoints.Length];
        nextSpawnIndex++;
        return t;
    }
}
