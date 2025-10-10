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

    // Lijst van kleuren die gebruikt worden voor spelers
    private readonly List<Color> allColors = new List<Color>
    {
        Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan
    };

    // Houd bij welke kleuren al zijn toegewezen
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
        if (sceneName != "GameScene") return;

        Debug.Log("[Server] Alle clients hebben GameScene geladen. Spawning players...");

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

        Transform spawn = spawnPoints.Length > 0 ? spawnPoints[spawnIndex % spawnPoints.Length] : new GameObject("DummySpawn").transform;

        GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation * Quaternion.Euler(0f, 180f, 0f));

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId, true);
        }
        else
        {
            Debug.LogError("Player prefab mist NetworkObject component!");
        }

        // Kleur toewijzen via NetworkVariable zodat iedereen het ziet
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

        Color color;
        if (availableColors.Count == 0)
        {
            color = Random.ColorHSV(); // fallback
        }
        else
        {
            color = availableColors[0];
        }

        assignedColors[clientId] = color;
        return color;
    }
}
