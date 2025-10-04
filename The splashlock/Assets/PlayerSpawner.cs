using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance;

    [Header("Prefab van de speler")]
    public GameObject playerPrefab;

    [Header("Spawnpunten in de scene")]
    public Transform[] spawnPoints;

    // Alle mogelijke kleuren
    private List<Color> allColors = new List<Color>
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    // Houd bij welke kleuren al in gebruik zijn (clientId -> kleur)
    private Dictionary<ulong, Color> playerColors = new Dictionary<ulong, Color>();

    private int nextSpawnIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Alleen server spawnt spelers
        if (!NetworkManager.Singleton.IsServer) return;

        SpawnPlayer(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Geef kleur vrij
        if (playerColors.ContainsKey(clientId))
            playerColors.Remove(clientId);
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints.Length == 0) return null;

        Transform spawn = spawnPoints[nextSpawnIndex % spawnPoints.Length];
        nextSpawnIndex++;
        return spawn;
    }

    private Color GetNextUniqueColor()
    {
        var usedColors = playerColors.Values.ToList();
        var availableColors = allColors.Except(usedColors).ToList();

        if (availableColors.Count == 0) return Color.white; // fallback

        return availableColors[0];
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is niet ingesteld!");
            return;
        }

        Transform spawnPoint = GetNextSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("Geen spawnpoints ingesteld!");
            return;
        }

        // Rotatie van het spawnpoint + 180 graden op Y-as
        Quaternion spawnRot = spawnPoint.rotation * Quaternion.Euler(0, 180f, 0);

        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnRot);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        // Alleen server bepaalt de kleur
        Color playerColorValue = GetNextUniqueColor();
        playerColors[clientId] = playerColorValue;

        Player playerScript = player.GetComponent<Player>();
        if (playerScript != null)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                playerScript.playerColor.Value = playerColorValue;
            }
        }
    }
}
