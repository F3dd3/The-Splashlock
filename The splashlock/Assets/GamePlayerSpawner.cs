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
        if (!NetworkManager.Singleton.IsServer) return;

        if (sceneName == "GameScene")
        {
            int spawnCounter = 0;
            foreach (ulong clientId in clientsCompleted)
            {
                clientSpawnIndex[clientId] = spawnCounter++;
                SpawnPlayerForClient(clientId, clientSpawnIndex[clientId]);
            }
            return;
        }

        if (sceneName == "MainLobby")
        {
            foreach (var obj in FindObjectsOfType<NetworkObject>())
            {
                if (obj.CompareTag("GamePlayer") && obj.IsSpawned)
                    obj.Despawn(true);
            }

            Back.Instance?.ResetReadyStatus();
            PlayerSpawner.Instance?.ResetAll();

            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                PlayerSpawner.Instance?.SpawnPlayer(clientId, true);
            }
        }
    }

    private void SpawnPlayerForClient(ulong clientId, int spawnIndex)
    {
        if (playerPrefab == null) return;

        Transform spawn = spawnPoints.Length > 0 ? spawnPoints[spawnIndex % spawnPoints.Length] : new GameObject("DummySpawn").transform;

        GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation);

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null) netObj.SpawnAsPlayerObject(clientId, true);

        GamePlayerColor colorScript = player.GetComponent<GamePlayerColor>();
        if (colorScript != null)
        {
            Color playerColor = GetNextUniqueColor(clientId);
            colorScript.SetColor(playerColor);
        }
    }

    private Color GetNextUniqueColor(ulong clientId)
    {
        var usedColors = assignedColors.Values.ToList();
        var availableColors = allColors.Except(usedColors).ToList();

        Color color = availableColors.Count > 0 ? availableColors[0] : Random.ColorHSV();
        assignedColors[clientId] = color;
        return color;
    }
}
