using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GamePlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Parkour Lines Tag")]
    public string raceLineTag = "RaceLine"; // Alle LineRenderer objecten moeten deze tag hebben

    private Dictionary<ulong, int> clientSpawnIndex = new Dictionary<ulong, int>();

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
    }

    private void OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
                                      List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        int spawnCounter = 0;
        foreach (ulong clientId in clientsCompleted)
        {
            clientSpawnIndex[clientId] = spawnCounter++;
            SpawnPlayerForClient(clientId, clientSpawnIndex[clientId]);
        }
    }

    private void SpawnPlayerForClient(ulong clientId, int spawnIndex)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab niet ingesteld!");
            return;
        }

        // Kies spawn point
        Transform spawn = spawnPoints.Length > 0 ? spawnPoints[spawnIndex % spawnPoints.Length] : new GameObject("DummySpawn").transform;

        // Spawn player
        GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation);
        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.SpawnAsPlayerObject(clientId, true);

        // Voeg LineRenderers toe aan PlayerProgressSpline
        PlayerProgressSpline progress = player.GetComponent<PlayerProgressSpline>();
        if (progress != null)
        {
            GameObject[] lineObjects = GameObject.FindGameObjectsWithTag(raceLineTag);
            LineRenderer[] lines = new LineRenderer[lineObjects.Length];
            for (int i = 0; i < lineObjects.Length; i++)
                lines[i] = lineObjects[i].GetComponent<LineRenderer>();

            progress.raceLines = lines;
        }
    }

    /// <summary>
    /// Optionele reset van alle players
    /// </summary>
    public void FullReset()
    {
        foreach (var kvp in clientSpawnIndex)
        {
            ulong clientId = kvp.Key;
            if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            {
                var client = NetworkManager.Singleton.ConnectedClients[clientId];
                if (client != null && client.PlayerObject != null)
                {
                    NetworkObject netObj = client.PlayerObject.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
                    Destroy(client.PlayerObject.gameObject);
                }
            }
        }
        clientSpawnIndex.Clear();
    }
}
