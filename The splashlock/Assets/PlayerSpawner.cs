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
    public readonly Dictionary<ulong, Color> playerColors = new Dictionary<ulong, Color>();
    [HideInInspector]
    public readonly Dictionary<ulong, Player> playerRefs = new Dictionary<ulong, Player>();

    private readonly Dictionary<ulong, int> playerSpawnIndices = new Dictionary<ulong, int>();
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
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (!playerRefs.ContainsKey(clientId))
            SpawnPlayer(clientId);

        UpdateUIVisibility();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        RemovePlayer(clientId);
        UpdateUIVisibility();
    }

    private void UpdateUIVisibility()
    {
        bool multiplePlayers = NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList.Count > 1;

        Back.Instance?.SetReadyStatsVisible(multiplePlayers);

        var lm = FindObjectOfType<LobbyManager>();
        if (lm != null && NetworkManager.Singleton != null)
        {
            lm.SetLeaveButtonVisible(NetworkManager.Singleton.IsHost && multiplePlayers);
        }
    }

    public void SpawnPlayer(ulong clientId)
    {
        if (playerRefs.ContainsKey(clientId) || playerPrefab == null) return;

        int spawnIndex = freeSpawnIndices.Count > 0 ? freeSpawnIndices[0] : nextSpawnIndex % spawnPoints.Length;
        if (freeSpawnIndices.Count > 0) freeSpawnIndices.RemoveAt(0);
        else nextSpawnIndex++;

        Vector3 spawnPos = spawnPoints[spawnIndex].position;

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        playerSpawnIndices[clientId] = spawnIndex;

        Color color = GetNextUniqueColor();
        playerColors[clientId] = color;

        Player playerScript = player.GetComponent<Player>();
        playerRefs[clientId] = playerScript;

        Vector3 colorVec = new Vector3(color.r, color.g, color.b);
        playerScript.SetColorServerRpc(colorVec);
        playerScript.ForceColorClientRpc(colorVec);
    }

    private Color GetNextUniqueColor()
    {
        var usedColors = playerColors.Values.ToList();
        var availableColors = allColors.Except(usedColors).ToList();
        return availableColors.Count > 0 ? availableColors[0] : Random.ColorHSV();
    }

    public void RemovePlayer(ulong clientId)
    {
        if (playerRefs.ContainsKey(clientId))
        {
            var netObj = playerRefs[clientId].GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned && (NetworkManager.Singleton.IsServer || netObj.IsOwner))
                netObj.Despawn(true);

            playerRefs.Remove(clientId);
        }

        if (playerColors.ContainsKey(clientId)) playerColors.Remove(clientId);

        if (playerSpawnIndices.ContainsKey(clientId))
        {
            freeSpawnIndices.Add(playerSpawnIndices[clientId]);
            playerSpawnIndices.Remove(clientId);
        }

        Back.Instance?.RemoveClientReadyStatus(clientId);
    }

    public void ClientLeave(ulong clientId)
    {
        RemovePlayer(clientId);
    }

    public void ResetAll()
    {
        nextSpawnIndex = 0;
        playerColors.Clear();
        playerRefs.Clear();
        playerSpawnIndices.Clear();
        freeSpawnIndices.Clear();
    }
}
