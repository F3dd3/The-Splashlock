using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("UI Elements")]
    public Button joinButton;
    public Button leaveButton;
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI infoText;

    [Header("Player Prefab & Spawn Points")]
    public GameObject playerPrefab;
    [HideInInspector] public Transform[] spawnPoints; // dynamisch gevuld

    [Header("Player Colors")]
    public List<Color> allColors = new List<Color>
        { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

    private readonly List<GameObject> allPlayerClones = new();
    private readonly List<bool> cloneOccupied = new();

    private bool servicesInitialized;
    private bool clonesReady;
    private string lastJoinCode = "";
    private bool hasJoinedOnce = false;

    private void Awake()
    {
        // Singleton + persistent
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeUnityServicesSafe();
    }

    private async void InitializeUnityServicesSafe()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            servicesInitialized = true;
            Debug.Log("[LobbyManager] Unity Services initialized.");
        }
        catch (Exception e)
        {
            Debug.LogError("[LobbyManager] Unity Services init failed: " + e.Message);
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (joinButton != null)
            joinButton.onClick.AddListener(() => _ = JoinGameAsync());
        if (leaveButton != null)
            leaveButton.onClick.AddListener(() => _ = LeaveFlowAsync());

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedHandler;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedHandler;
        }

        WaitUntilReadyAndAutoHost();
    }

    private async void WaitUntilReadyAndAutoHost()
    {
        await WaitUntilServicesReadyAsync();

        if (!hasJoinedOnce && SceneManager.GetActiveScene().name == "MainLobby")
        {
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                AutoHostGame();
            }
        }
    }

    private async Task WaitUntilServicesReadyAsync()
    {
        while (!servicesInitialized)
            await Task.Yield();
    }

    // 🔁 Scene geladen: spawnpoints zoeken & clones spawnen
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[LobbyManager] Scene loaded: {scene.name}");

        // spawnpoints opnieuw zoeken
        var foundPoints = GameObject.FindGameObjectsWithTag("SpawnPoint")
                                    .Select(go => go.transform)
                                    .ToArray();
        spawnPoints = foundPoints;
        Debug.Log($"[LobbyManager] Found {spawnPoints.Length} spawn points.");

        if (scene.name == "MainLobby")
        {
            clonesReady = true;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                SpawnAllPlayerClones();
                AssignExistingClients();
            }
        }
    }

    private async void AutoHostGame()
    {
        await WaitUntilServicesReadyAsync();

        try
        {
            ResetServerData();

            var allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            lastJoinCode = joinCode;

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            Debug.Log("[LobbyManager] Host started successfully.");

            await Task.Delay(100);
            SpawnAllPlayerClones();

            infoText.text = $"Join code: {lastJoinCode}";
            hasJoinedOnce = true;
            clonesReady = true;
        }
        catch (Exception e)
        {
            Debug.LogError("[LobbyManager] Auto-host failed: " + e.Message);
            infoText.text = "Auto-host failed!";
        }
    }

    private async Task JoinGameAsync()
    {
        string joinCode = joinCodeInput != null ? joinCodeInput.text.Trim() : "";
        if (string.IsNullOrEmpty(joinCode))
        {
            infoText.text = "Invalid code!";
            await Task.Delay(2000);
            infoText.text = $"Join code: {lastJoinCode}";
            return;
        }

        try
        {
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            infoText.text = $"Connected to: {joinCode}";
            hasJoinedOnce = true;
        }
        catch
        {
            infoText.text = "Join failed!";
        }
    }

    private async Task LeaveFlowAsync()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            await HandleBackToLobbyAsync();
        }
    }

    // 🧭 Back to Lobby
    public async Task HandleBackToLobbyAsync()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log("[LobbyManager] Host BackToLobby started.");

        await SafeShutdownNetworkManagerAsync();
        await Task.Delay(300);

        var allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        lastJoinCode = joinCode;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );

        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.SceneManager.LoadScene("MainLobby", LoadSceneMode.Single);
    }

    private async Task SafeShutdownNetworkManagerAsync()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            while (NetworkManager.Singleton.IsListening)
                await Task.Yield();
        }
    }

    // 🧱 Spawning
    private void SpawnAllPlayerClones()
    {
        cloneOccupied.Clear();
        allPlayerClones.Clear();

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[LobbyManager] No spawnpoints found, skipping clone spawn.");
            return;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            var point = spawnPoints[i];
            if (point == null) continue;

            var playerObj = Instantiate(playerPrefab, point.position, Quaternion.identity);
            var playerScript = playerObj.GetComponent<Player>();
            playerScript.isVisible.Value = false;
            playerScript.isHostPlayer.Value = false;
            playerScript.ownerClientId.Value = 0UL;

            var netObj = playerObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            Color color = allColors[i % allColors.Count];
            playerScript.SetColorServerRpc(new Vector3(color.r, color.g, color.b));

            allPlayerClones.Add(playerObj);
            cloneOccupied.Add(false);
        }

        Debug.Log($"[LobbyManager] Spawned {allPlayerClones.Count} clones.");
    }

    private void AssignExistingClients()
    {
        var sortedClients = NetworkManager.Singleton.ConnectedClientsList.OrderBy(c => c.ClientId).ToList();
        for (int i = 0; i < sortedClients.Count && i < allPlayerClones.Count; i++)
        {
            AssignNextCloneToClient(sortedClients[i].ClientId);
        }
    }

    private void AssignNextCloneToClient(ulong clientId)
    {
        int nextIndex = cloneOccupied.FindIndex(o => o == false);
        if (nextIndex == -1) return;

        var clone = allPlayerClones[nextIndex];
        var playerScript = clone.GetComponent<Player>();

        playerScript.ownerClientId.Value = clientId;
        playerScript.isVisible.Value = true;
        cloneOccupied[nextIndex] = true;

        Debug.Log($"[LobbyManager] Assigned clone {nextIndex} to client {clientId}");
    }

    private void OnClientDisconnectedHandler(ulong clientId)
    {
        if (!IsServer) return;

        GameObject clone = allPlayerClones.FirstOrDefault(c =>
            c != null && c.GetComponent<Player>().ownerClientId.Value == clientId);

        if (clone != null)
        {
            int index = allPlayerClones.IndexOf(clone);
            clone.GetComponent<Player>().isVisible.Value = false;
            cloneOccupied[index] = false;
        }
    }

    private async void OnClientConnectedHandler(ulong clientId)
    {
        if (!IsServer) return;
        while (!clonesReady) await Task.Yield();
        AssignNextCloneToClient(clientId);
    }

    private void ResetServerData()
    {
        allPlayerClones.Clear();
        cloneOccupied.Clear();
        clonesReady = false;
    }
}
