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
        bool multiplePlayers = NetworkManager.Singleton.ConnectedClientsList.Count > 1;

        if (Back.Instance != null)
            Back.Instance.SetReadyStatsVisible(multiplePlayers);

        var lm = FindObjectOfType<LobbyManager>();
        if (lm != null)
            lm.SetLeaveButtonVisible(NetworkManager.Singleton.IsHost && multiplePlayers);
    }

    public void SpawnPlayer(ulong clientId)
    {
        if (playerRefs.ContainsKey(clientId) || playerPrefab == null) return;

        Vector3 spawnPos = GetNextSpawnPosition();
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        Color color = GetNextUniqueColor();
        playerColors[clientId] = color;

        Player playerScript = player.GetComponent<Player>();
        playerRefs[clientId] = playerScript;

        Vector3 colorVec = new Vector3(color.r, color.g, color.b);
        playerScript.SetColorServerRpc(colorVec);
        playerScript.ForceColorClientRpc(colorVec);
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
        return availableColors.Count > 0 ? availableColors[0] : Random.ColorHSV();
    }

    public void RemovePlayer(ulong clientId)
    {
        if (playerRefs.ContainsKey(clientId))
        {
            var netObj = playerRefs[clientId].GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                if (NetworkManager.Singleton.IsServer || netObj.IsOwner)
                    netObj.Despawn(true);
            }
            playerRefs.Remove(clientId);
        }

        if (playerColors.ContainsKey(clientId))
            playerColors.Remove(clientId);

        Back.Instance?.RemoveClientReadyStatus(clientId);
    }

    public void ClientLeave(ulong clientId)
    {
        RemovePlayer(clientId);
        ResetSpawnPoints();
    }

    public void ResetSpawnPoints()
    {
        nextSpawnIndex = 0;
        playerColors.Clear();
        playerRefs.Clear();
    }
}
