using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    [Header("UI Elements")]
    public Button joinButton;
    public Button leaveButton;
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI infoText;

    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawn Points (assign in inspector)")]
    public Transform[] spawnPoints;

    [Header("Player Colors")]
    public List<Color> allColors = new List<Color>
        { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

    private List<GameObject> allPlayerClones = new List<GameObject>();
    private List<bool> cloneOccupied = new List<bool>();

    private bool servicesInitialized = false;
    private string lastJoinCode = "";
    private bool hasJoinedOnce = false;
    private bool clonesReady = false;

    private void Awake()
    {
        InitializeUnityServicesSafe();
    }

    private async void InitializeUnityServicesSafe()
    {
        try
        {
            await UnityServices.InitializeAsync();
            servicesInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Services init failed: " + e.Message);
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        infoText.gameObject.SetActive(true);

        joinButton.onClick.AddListener(() => _ = JoinGameAsync());
        leaveButton.onClick.AddListener(() => _ = LeaveFlowAsync());
        leaveButton.gameObject.SetActive(false);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedHandler;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedHandler;
        }

        WaitUntilReadyAndAutoHost();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainLobby")
        {
            Destroy(gameObject); // LobbyManager niet nodig in game scene
            return;
        }

        // Lobby logic alleen in MainLobby
        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogWarning("LobbyManager: spawnPoints niet ingesteld in inspector");

        if (!NetworkManager.Singleton.IsListening && servicesInitialized)
            AutoHostGame();
    }

    private async void WaitUntilReadyAndAutoHost()
    {
        await WaitUntilServicesReadyAsync();
        if (!hasJoinedOnce && SceneManager.GetActiveScene().name == "MainLobby")
            AutoHostGame();
    }

    private async Task WaitUntilServicesReadyAsync()
    {
        while (!servicesInitialized)
            await Task.Yield();

        while (!AuthenticationService.Instance.IsSignedIn || string.IsNullOrEmpty(AuthenticationService.Instance.PlayerId))
        {
            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch { }
            await Task.Yield();
        }
    }

    private async Task SafeShutdownNetworkManagerAsync()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            // Wacht tot NetworkManager echt klaar is
            while (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                await Task.Yield();
        }
    }

    public async void AutoHostGame()
    {
        await WaitUntilServicesReadyAsync();

        // Veilig shutdown
        await SafeShutdownNetworkManagerAsync();

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Cannot start host: NetworkManager still running");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Cannot start host: spawnPoints not set");
            return;
        }

        try
        {
            ResetServerData();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            lastJoinCode = joinCode;

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();

            await Task.Delay(100); // kleine delay zodat scene ready is
            SpawnAllPlayerClones();

            infoText.text = $"Join code: {lastJoinCode}";
            leaveButton.gameObject.SetActive(false);
            hasJoinedOnce = true;
            clonesReady = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Auto-host failed: " + e.Message);
            infoText.text = "Auto-host failed!";
        }
    }

    private async Task JoinGameAsync()
    {
        string joinCode = joinCodeInput.text.Trim();
        if (string.IsNullOrEmpty(joinCode))
        {
            await ShowInvalidCodeTemporarily();
            return;
        }

        await WaitUntilServicesReadyAsync();
        await JoinRelayAsync(joinCode);
    }

    private async Task JoinRelayAsync(string joinCode)
    {
        JoinAllocation allocation;
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch
        {
            await ShowInvalidCodeTemporarily();
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            foreach (var clone in allPlayerClones)
                if (clone != null) Destroy(clone);
            allPlayerClones.Clear();
            await SafeShutdownNetworkManagerAsync();
            await Task.Delay(300);
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData
        );

        NetworkManager.Singleton.StartClient();
        await WaitUntilClientConnectedAsync();

        infoText.text = $"Connected to: {joinCode}";
        leaveButton.gameObject.SetActive(false);
        hasJoinedOnce = true;
    }

    private async Task WaitUntilClientConnectedAsync()
    {
        while (NetworkManager.Singleton == null ||
               !NetworkManager.Singleton.IsClient ||
               NetworkManager.Singleton.LocalClient == null)
        {
            await Task.Yield();
        }
    }

    private async Task ShowInvalidCodeTemporarily()
    {
        infoText.text = "Invalid code!";
        await Task.Delay(2000);
        infoText.text = $"Join code: {lastJoinCode}";
    }

    private async Task LeaveFlowAsync()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList.ToList())
            {
                if (client.ClientId != NetworkManager.Singleton.LocalClientId)
                    NetworkManager.Singleton.DisconnectClient(client.ClientId);
            }
        }

        await HandleClientOrHostLeftAsync();
    }

    public async Task HandleClientOrHostLeftAsync()
    {
        // Veilig shutdown NetworkManager
        await SafeShutdownNetworkManagerAsync();

        ResetServerData();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainLobby");
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
            await Task.Yield();

        await WaitUntilServicesReadyAsync();

        if (!NetworkManager.Singleton.IsListening)
            AutoHostGame();
    }

    private void OnClientDisconnectedHandler(ulong clientId)
    {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("Disconnected from host! Returning to lobby...");
                _ = HandleClientOrHostLeftAsync();
            }
        }

        if (NetworkManager.Singleton.IsServer)
        {
            GameObject clone = allPlayerClones.FirstOrDefault(c =>
                c.activeSelf && c.GetComponent<Player>().ownerClientId.Value == clientId);
            if (clone != null) clone.GetComponent<Player>().isVisible.Value = false;

            int index = allPlayerClones.FindIndex(c => c == clone);
            if (index != -1) cloneOccupied[index] = false;
        }
    }

    private async void OnClientConnectedHandler(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        while (!clonesReady) await Task.Yield();

        AssignNextCloneToClient(clientId);
    }

    private void AssignNextCloneToClient(ulong clientId)
    {
        int nextIndex = cloneOccupied.FindIndex(o => o == false);
        if (nextIndex == -1)
        {
            Debug.LogWarning("Geen vrije player clone beschikbaar voor client: " + clientId);
            return;
        }

        GameObject clone = allPlayerClones[nextIndex];
        Player playerScript = clone.GetComponent<Player>();

        playerScript.ownerClientId.Value = clientId;
        playerScript.isVisible.Value = true;

        cloneOccupied[nextIndex] = true;

        playerScript.ownerClientId.SetDirty(true);
        playerScript.isVisible.SetDirty(true);

        Debug.Log($"Clone {nextIndex} toegewezen aan client {clientId}");
    }

    private void SpawnAllPlayerClones()
    {
        cloneOccupied.Clear();
        allPlayerClones.Clear();

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Geen spawn points ingesteld! Clones niet gespawnd.");
            return;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject playerObj = Instantiate(playerPrefab, spawnPoints[i].position, Quaternion.Euler(0, 180, 0));
            Player playerScript = playerObj.GetComponent<Player>();

            playerScript.isVisible.Value = false;
            playerScript.isHostPlayer.Value = false;
            playerScript.ownerClientId.Value = 0UL;

            NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            Color color = allColors[i % allColors.Count];
            playerScript.SetColorServerRpc(new Vector3(color.r, color.g, color.b));

            allPlayerClones.Add(playerObj);
            cloneOccupied.Add(false);
        }

        Debug.Log($"Spawned {allPlayerClones.Count} player clones.");
        clonesReady = true;
    }

    private void ResetServerData()
    {
        allPlayerClones.Clear();
        cloneOccupied.Clear();
        clonesReady = false;
    }
}
