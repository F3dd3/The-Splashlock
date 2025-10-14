using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance;

    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [HideInInspector]
    public readonly List<Color> allColors = new List<Color>
    { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

    [HideInInspector]
    public readonly Dictionary<string, Color> playerColors = new Dictionary<string, Color>();
    [HideInInspector]
    public readonly Dictionary<string, Player> playerRefs = new Dictionary<string, Player>();

    private readonly Dictionary<string, int> playerIdToSpawnIndex = new Dictionary<string, int>();
    private readonly List<int> freeSpawnIndices = new List<int>();
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
        if (!NetworkManager.Singleton.IsServer) return;

        string playerId = LobbyManager.GetPlayerIdFromClientId(clientId);

        if (!playerRefs.ContainsKey(playerId))
            SpawnPlayerWithPlayerId(clientId, playerId);

        UpdateUIVisibility();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        string playerId = LobbyManager.GetPlayerIdFromClientId(clientId);
        RemovePlayerByPlayerId(playerId);
        UpdateUIVisibility();
    }

    private void UpdateUIVisibility()
    {
        bool multiplePlayers = NetworkManager.Singleton.ConnectedClientsList.Count > 1;

        var lm = FindObjectOfType<LobbyManager>();
        if (lm != null)
            lm.SetLeaveButtonVisible(NetworkManager.Singleton.IsHost && multiplePlayers);
    }

    public void SpawnPlayerWithPlayerId(ulong clientId, string playerId)
    {
        if (playerRefs.ContainsKey(playerId) || playerPrefab == null) return;

        int spawnIndex;
        if (playerIdToSpawnIndex.ContainsKey(playerId))
        {
            spawnIndex = playerIdToSpawnIndex[playerId];
        }
        else
        {
            spawnIndex = GetNextFreeSpawnIndex();
            playerIdToSpawnIndex[playerId] = spawnIndex;
        }

        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        // Kleur
        Color color = GetNextUniqueColor(playerId);
        playerColors[playerId] = color;

        Player playerScript = player.GetComponent<Player>();
        playerRefs[playerId] = playerScript;

        Vector3 colorVec = new Vector3(color.r, color.g, color.b);
        playerScript.SetColorServerRpc(colorVec);
        playerScript.ForceColorClientRpc(colorVec);
    }

    private int GetNextFreeSpawnIndex()
    {
        if (freeSpawnIndices.Count > 0)
        {
            int idx = freeSpawnIndices[0];
            freeSpawnIndices.RemoveAt(0);
            return idx;
        }
        else
        {
            int idx = nextSpawnIndex % spawnPoints.Length;
            nextSpawnIndex++;
            return idx;
        }
    }

    private Color GetNextUniqueColor(string playerId)
    {
        var usedColors = playerColors.Values.ToList();
        var availableColors = allColors.Except(usedColors).ToList();
        return availableColors.Count > 0 ? availableColors[0] : Random.ColorHSV();
    }

    public void RemovePlayerByPlayerId(string playerId)
    {
        if (!playerRefs.ContainsKey(playerId)) return;

        var netObj = playerRefs[playerId].GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            if (NetworkManager.Singleton.IsServer || netObj.IsOwner)
                netObj.Despawn(true);
        }
        playerRefs.Remove(playerId);

        if (playerColors.ContainsKey(playerId))
            playerColors.Remove(playerId);

        if (playerIdToSpawnIndex.ContainsKey(playerId))
        {
            freeSpawnIndices.Add(playerIdToSpawnIndex[playerId]);
            playerIdToSpawnIndex.Remove(playerId);
        }
    }

    public void ClientLeave(string playerId)
    {
        RemovePlayerByPlayerId(playerId);
    }

    public void ResetSpawnPoints()
    {
        nextSpawnIndex = 0;
        playerColors.Clear();
        playerRefs.Clear();
        playerIdToSpawnIndex.Clear();
        freeSpawnIndices.Clear();
    }
}
