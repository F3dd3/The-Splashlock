using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class GamePlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawnpoints in Scene")]
    public Transform[] spawnPoints;

    private readonly List<Color> allColors = new List<Color>
    {
        Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan
    };

    private readonly Dictionary<ulong, Color> assignedColors = new Dictionary<ulong, Color>();
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
        // Verwijder alle lobby clones
        var lobbyClones = FindObjectsOfType<Player>(true)
                           .Where(p => p.isLobbyClone)
                           .ToList();

        foreach (var clone in lobbyClones)
        {
            NetworkObject netObj = clone.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned && NetworkManager.Singleton.IsServer)
                netObj.Despawn(true);

            Destroy(clone.gameObject);
        }

        Debug.Log($"Alle lobby clones verwijderd: {lobbyClones.Count}");

        // Alleen de server spawnt nieuwe players
        if (!NetworkManager.Singleton.IsServer) return;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // Zoek automatisch spawnPoints op in scene
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (spawns.Length == 0)
            {
                Debug.LogError("[GamePlayerSpawner] Geen spawnPoints gevonden in scene! Players kunnen niet spawnen.");
                return;
            }

            spawnPoints = spawns.OrderBy(go => go.name).Select(go => go.transform).ToArray();
            Debug.Log($"[GamePlayerSpawner] {spawnPoints.Length} spawnPoints gevonden en gesorteerd.");
        }

        int spawnCounter = 0;
        foreach (ulong clientId in clientsCompleted)
        {
            int spawnIndex = spawnCounter % spawnPoints.Length;
            clientSpawnIndex[clientId] = spawnIndex;
            SpawnPlayerForClient(clientId, spawnIndex);
            spawnCounter++;
        }

        // Hide loading screen nadat alle players gespawnd zijn
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideLoadingScreenClientRpc();
        }
    }

    private void SpawnPlayerForClient(ulong clientId, int spawnIndex)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab niet ingesteld!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError($"SpawnPoints nog niet beschikbaar voor client {clientId}. Player wordt niet gespawned.");
            return;
        }

        spawnIndex = Mathf.Clamp(spawnIndex, 0, spawnPoints.Length - 1);
        Transform spawn = spawnPoints[spawnIndex];

        if (spawn == null)
        {
            Debug.LogError($"SpawnPoint index {spawnIndex} is null voor client {clientId}. Player wordt niet gespawned.");
            return;
        }

        GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation);

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.SpawnAsPlayerObject(clientId, true);

        Player playerScript = player.GetComponent<Player>();
        if (playerScript != null)
            playerScript.isLobbyClone = false;

        GamePlayerColor colorScript = player.GetComponent<GamePlayerColor>();
        if (colorScript != null)
        {
            Color playerColor = GetNextUniqueColor(clientId);
            colorScript.SetColor(playerColor);
        }

        Debug.Log($"Client {clientId} gespawned op spawnIndex {spawnIndex} ({spawn.position})");
    }

    private Color GetNextUniqueColor(ulong clientId)
    {
        var usedColors = assignedColors.Values.ToList();
        var availableColors = allColors.Except(usedColors).ToList();

        Color color = availableColors.Count > 0 ? availableColors[0] : Random.ColorHSV();
        assignedColors[clientId] = color;
        return color;
    }

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
        assignedColors.Clear();
    }
}
