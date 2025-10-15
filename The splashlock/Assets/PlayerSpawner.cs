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
    private readonly Dictionary<ulong, Color> rejoinColors = new Dictionary<ulong, Color>();
    private readonly HashSet<int> freeSpawnPoints = new HashSet<int>();

    private int nextSpawnIndex = 1; // Host spawnpoint 0, clients vanaf 1

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

        SpawnPlayer(clientId, true);
        UpdateUIVisibility();

        // Stuur kleuren van alle spelers naar de nieuwe client
        foreach (var kvp in playerRefs)
        {
            ulong otherId = kvp.Key;
            Player otherPlayer = kvp.Value;
            Vector3 colorVec = new Vector3(playerColors[otherId].r, playerColors[otherId].g, playerColors[otherId].b);

            otherPlayer.ForceColorClientRpc(colorVec, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            });
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (!playerSpawnIndices.ContainsKey(clientId)) return;

        int freedIndex = playerSpawnIndices[clientId];

        // Voeg spawnpoint toe aan vrije punten
        if (freedIndex != 0) // host blijft op 0
            freeSpawnPoints.Add(freedIndex);

        // Sla kleur op voor rejoin
        if (playerColors.ContainsKey(clientId))
            rejoinColors[clientId] = playerColors[clientId];

        RemovePlayer(clientId);
        UpdateUIVisibility();
    }

    public void SpawnPlayer(ulong clientId, bool forceSpawn = false)
    {
        if (!forceSpawn && playerRefs.ContainsKey(clientId)) return;
        if (playerPrefab == null) return;

        if (playerRefs.ContainsKey(clientId))
            RemovePlayer(clientId); // force cleanup

        int spawnIndex;

        // Host spawnt altijd op 0
        if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            spawnIndex = 0;
        }
        else
        {
            // Gebruik eerst vrije spawnpoints
            if (freeSpawnPoints.Count > 0)
            {
                spawnIndex = freeSpawnPoints.Min();
                freeSpawnPoints.Remove(spawnIndex);
            }
            else
            {
                spawnIndex = ((nextSpawnIndex - 1) % (spawnPoints.Length - 1)) + 1;
                nextSpawnIndex++;
            }
        }

        Vector3 spawnPos = spawnPoints[spawnIndex].position;

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        playerSpawnIndices[clientId] = spawnIndex;

        Color color;

        // Als returning client, gebruik oude kleur
        if (rejoinColors.ContainsKey(clientId))
        {
            color = rejoinColors[clientId];
            rejoinColors.Remove(clientId);
        }
        else
        {
            color = GetNextUniqueColor();
        }

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
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn(true);

            playerRefs.Remove(clientId);
        }

        if (playerColors.ContainsKey(clientId)) playerColors.Remove(clientId);
        if (playerSpawnIndices.ContainsKey(clientId)) playerSpawnIndices.Remove(clientId);

        Back.Instance?.RemoveClientReadyStatus(clientId);
    }

    public void ClientLeave(ulong clientId)
    {
        RemovePlayer(clientId);
    }

    public void ResetAll()
    {
        nextSpawnIndex = 1;
        playerColors.Clear();
        playerRefs.Clear();
        playerSpawnIndices.Clear();
        freeSpawnPoints.Clear();
        rejoinColors.Clear();
    }

    private void UpdateUIVisibility()
    {
        bool multiplePlayers = NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList.Count > 1;

        Back.Instance?.SetReadyStatsVisible(multiplePlayers);

        var lm = FindObjectOfType<LobbyManager>();
        if (lm != null && NetworkManager.Singleton != null)
            lm.SetLeaveButtonVisible(NetworkManager.Singleton.IsHost && multiplePlayers);
    }
}
