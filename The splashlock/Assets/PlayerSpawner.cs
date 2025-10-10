using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance;

    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawnpoints")]
    public Transform[] spawnPoints;

    private readonly List<Color> allColors = new List<Color>
    {
        Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan
    };

    private readonly Dictionary<ulong, Color> playerColors = new Dictionary<ulong, Color>();
    private readonly Dictionary<ulong, Player> playerRefs = new Dictionary<ulong, Player>();
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
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Debug.Log($"[SERVER] Client {clientId} connected. Spawning...");

        SpawnPlayer(clientId);

        // ⬇️ Stuur alle bestaande kleuren opnieuw naar de nieuwe client
        foreach (var kvp in playerRefs)
        {
            ulong id = kvp.Key;
            Player p = kvp.Value;

            if (p != null && playerColors.ContainsKey(id))
            {
                Vector3 colVec = new Vector3(playerColors[id].r, playerColors[id].g, playerColors[id].b);
                p.ForceColorClientRpc(colVec, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { clientId } // stuur enkel naar de nieuwe speler
                    }
                });
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (playerColors.ContainsKey(clientId))
            playerColors.Remove(clientId);
        if (playerRefs.ContainsKey(clientId))
            playerRefs.Remove(clientId);
    }

    private Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints.Length == 0) return Vector3.zero;
        Vector3 pos = spawnPoints[nextSpawnIndex % spawnPoints.Length].position;
        nextSpawnIndex++;
        return pos;
    }

    private Color GetNextUniqueColor()
    {
        var usedColors = playerColors.Values.ToList();
        var availableColors = allColors.Except(usedColors).ToList();
        if (availableColors.Count == 0)
            return Random.ColorHSV();
        return availableColors[0];
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("❌ Player prefab niet ingesteld!");
            return;
        }

        Vector3 spawnPos = GetNextSpawnPosition();
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.Euler(0f, 180f, 0f));
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        // Unieke kleur kiezen
        Color playerColorValue = GetNextUniqueColor();
        playerColors[clientId] = playerColorValue;

        // Koppeling bijhouden
        Player playerScript = player.GetComponent<Player>();
        playerRefs[clientId] = playerScript;

        // Stel kleur in (server + clients)
        Vector3 colorVec = new Vector3(playerColorValue.r, playerColorValue.g, playerColorValue.b);
        playerScript.SetColorServerRpc(colorVec);
        playerScript.ForceColorClientRpc(colorVec);
    }
}
