using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance;

    [Header("Prefabs & Spawn Points")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Player Colors")]
    public readonly List<Color> allColors = new List<Color>
    { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

    public readonly Dictionary<ulong, Color> playerColors = new Dictionary<ulong, Color>();
    public readonly Dictionary<ulong, Player> playerRefs = new Dictionary<ulong, Player>();

    private readonly Dictionary<ulong, int> playerSpawnIndices = new Dictionary<ulong, int>();
    private readonly Dictionary<ulong, Color> rejoinColors = new Dictionary<ulong, Color>();
    private readonly HashSet<int> freeSpawnPoints = new HashSet<int>();

    private int nextSpawnIndex = 1; // 0 = host, clients vanaf 1
    private int nextColorIndex = 0; // eerste kleur voor host

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

        // spawn hoofd PlayerObject
        SpawnPlayer(clientId, true);

        // sync kleuren naar nieuwe client
        foreach (var kvp in playerRefs)
        {
            ulong otherId = kvp.Key;
            Player otherPlayer = kvp.Value;

            if (playerColors.TryGetValue(otherId, out Color color))
            {
                Vector3 colorVec = new Vector3(color.r, color.g, color.b);
                otherPlayer.ForceColorClientRpc(colorVec, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
                });
            }
        }

        // sync ready status
        foreach (var kvp in playerRefs)
        {
            Player existingPlayer = kvp.Value;
            existingPlayer.ForceReadyClientRpc(existingPlayer.isReady.Value, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            });
        }

        CheckAllPlayersSpawned();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (!playerSpawnIndices.ContainsKey(clientId)) return;

        int freedIndex = playerSpawnIndices[clientId];
        if (freedIndex != 0) freeSpawnPoints.Add(freedIndex);

        if (playerColors.ContainsKey(clientId))
            rejoinColors[clientId] = playerColors[clientId];

        RemovePlayer(clientId);
    }

    public void SpawnPlayer(ulong clientId, bool isMainPlayer = false)
    {
        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning($"NetworkManager niet ready, spawn van {clientId} uitgesteld");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("PlayerPrefab niet ingesteld!");
            return;
        }

        int spawnIndex;
        Color color;

        // Bepaal of dit een nieuwe client is
        bool isNewClient = !playerSpawnIndices.ContainsKey(clientId);

        if (isNewClient)
        {
            // Host
            if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
            {
                spawnIndex = 0;
                color = allColors[0];
                nextColorIndex = 1;
            }
            else if (freeSpawnPoints.Count > 0)
            {
                spawnIndex = freeSpawnPoints.Min();
                freeSpawnPoints.Remove(spawnIndex);
                color = allColors[nextColorIndex % allColors.Count];
                nextColorIndex++;
            }
            else
            {
                spawnIndex = nextSpawnIndex;
                nextSpawnIndex++;
                if (nextSpawnIndex >= spawnPoints.Length) nextSpawnIndex = 1; // start opnieuw bij 1
                color = allColors[nextColorIndex % allColors.Count];
                nextColorIndex++;
            }

            playerSpawnIndices[clientId] = spawnIndex;
            playerColors[clientId] = color;
        }
        else
        {
            // Extra clone van bestaande client
            spawnIndex = playerSpawnIndices[clientId];
            color = playerColors[clientId];
        }

        Vector3 spawnPos = spawnPoints[Mathf.Clamp(spawnIndex, 0, spawnPoints.Length - 1)].position;
        GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
        var netObj = playerObj.GetComponent<NetworkObject>();

        if (isMainPlayer)
            netObj.SpawnAsPlayerObject(clientId, true);
        else
            netObj.Spawn();

        Player playerScript = playerObj.GetComponent<Player>();

        // alleen hoofdobject tracken
        if (isNewClient)
            playerRefs[clientId] = playerScript;

        Vector3 colorVec = new Vector3(color.r, color.g, color.b);
        playerScript.SetColorServerRpc(colorVec);
        playerScript.ForceColorClientRpc(colorVec);

        if (clientId == NetworkManager.Singleton.LocalClientId && playerScript.nameLabel != null)
            playerScript.nameLabel.text = "You";
    }

    public void RemovePlayer(ulong clientId)
    {
        if (playerRefs.ContainsKey(clientId))
        {
            var netObj = playerRefs[clientId].GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
            playerRefs.Remove(clientId);
        }

        if (playerColors.ContainsKey(clientId)) playerColors.Remove(clientId);
        if (playerSpawnIndices.ContainsKey(clientId)) playerSpawnIndices.Remove(clientId);

        Back.Instance?.RemoveClientReadyStatus(clientId);
    }

    public void ResetAll()
    {
        nextSpawnIndex = 1;
        nextColorIndex = 0;
        playerColors.Clear();
        playerRefs.Clear();
        playerSpawnIndices.Clear();
        freeSpawnPoints.Clear();
        rejoinColors.Clear();
    }

    public void ResetForLobby()
    {
        foreach (var kvp in playerRefs)
        {
            ulong clientId = kvp.Key;
            Player player = kvp.Value;

            int spawnIndex = playerSpawnIndices.ContainsKey(clientId) ? playerSpawnIndices[clientId] : 0;
            spawnIndex = Mathf.Clamp(spawnIndex, 0, spawnPoints.Length - 1);

            player.transform.position = spawnPoints[spawnIndex].position;
            player.transform.rotation = Quaternion.Euler(0, 180, 0);

            if (playerColors.TryGetValue(clientId, out Color color))
            {
                Vector3 colorVec = new Vector3(color.r, color.g, color.b);
                player.SetColorServerRpc(colorVec);
                player.ForceColorClientRpc(colorVec);
            }
        }

        CheckAllPlayersSpawned();
    }

    private void CheckAllPlayersSpawned()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer) return;

        int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;
        if (playerRefs.Count >= totalPlayers && totalPlayers > 0)
        {
            if (LoadingScreenManager.Instance != null)
                LoadingScreenManager.Instance.HideLoadingScreenClientRpc();
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLobbySceneLoaded;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLobbySceneLoaded;
    }

    private void OnLobbySceneLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadMode,
                                    List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (sceneName == "MainLobby")
        {
            Debug.Log("[PlayerSpawner] Lobbyscene geladen, spelers worden teruggezet.");
            ResetForLobby();

            if (LoadingScreenManager.Instance != null)
                LoadingScreenManager.Instance.HideLoadingScreenClientRpc();
        }
    }
}
