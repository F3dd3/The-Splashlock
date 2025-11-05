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

    [Header("Player Prefab & Spawn Points")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Player Colors")]
    public List<Color> allColors = new List<Color> { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

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
        if (scene.name == "MainLobby")
        {
            clonesReady = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (LoadingScreenManager.Instance != null)
                LoadingScreenManager.Instance.HideLoadingScreenClientRpc();

            // Alleen de nieuwe LobbyManager spawnt clones en wijst ze toe
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("[LobbyManager] Nieuwe LobbyManager detecteert scene load MainLobby.");
                SpawnClonesAfterServicesReady();
            }
        }
        else
        {
            if (gameObject.scene.name != scene.name)
            {
                Destroy(gameObject);
            }
        }
    }

    private async void SpawnClonesAfterServicesReady()
    {
        spawnPoints = null;

        while (!servicesInitialized ||
               !AuthenticationService.Instance.IsSignedIn ||
               string.IsNullOrEmpty(AuthenticationService.Instance.PlayerId))
        {
            await Task.Yield();
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            GameObject[] spawns = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (spawns.Length == 0)
            {
                Debug.LogError("[LobbyManager] Geen spawnpoints gevonden in de huidige scene!");
                return;
            }

            // Sorteer de spawnpoints op naam (spawnpoint0, spawnpoint1, ...)
            spawnPoints = spawns
                .OrderBy(go => go.name)
                .Select(go => go.transform)
                .ToArray();

            Debug.Log($"[LobbyManager] {spawnPoints.Length} spawnPoints automatisch gedetecteerd en gesorteerd.");
        }

        try
        {
            ResetServerData();
            SpawnAllPlayerClones();
            Debug.Log("[LobbyManager] Player clones succesvol gespawned door nieuwe LobbyManager.");

            // ✅ Wijs clones toe aan reeds verbonden clients
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                AssignNextCloneToClient(client.ClientId);
            }
            Debug.Log("[LobbyManager] Player clones toegewezen aan alle connected clients.");
        }
        catch (Exception e)
        {
            Debug.LogError("[LobbyManager] Fout bij spawnen of toewijzen van player clones: " + e.Message);
        }
    }

    private async void WaitUntilReadyAndAutoHost()
    {
        await WaitUntilServicesReadyAsync();

        if (!hasJoinedOnce &&
            NetworkManager.Singleton != null &&
            !NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsServer &&
            SceneManager.GetActiveScene().name == "MainLobby")
        {
            AutoHostGame();
        }
    }

    private async Task WaitUntilServicesReadyAsync()
    {
        while (!servicesInitialized) await Task.Yield();

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
            while (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                await Task.Yield();
        }
    }

    public async void AutoHostGame()
    {
        await WaitUntilServicesReadyAsync();
        await SafeShutdownNetworkManagerAsync();

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Cannot start host: NetworkManager still running");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Cannot start host: spawnPoints niet ingesteld of niet gedetecteerd");
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

            await Task.Delay(100);
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
            await HandleClientOrHostLeftAsync();
        }
    }

    public async Task HandleClientOrHostLeftAsync()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[LobbyManager] Host terug naar lobby");

            NetworkManager.Singleton.SceneManager.LoadScene("MainLobby", LoadSceneMode.Single);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.Log("[LobbyManager] Client blijft verbonden bij host lobby.");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnClientDisconnectedHandler(ulong clientId)
    {
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
        if (nextIndex == -1) return;

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

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogError($"[LobbyManager] SpawnPoint index {i} is null, clone kan niet gespawned worden!");
                continue;
            }

            try
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
            catch (Exception e)
            {
                Debug.LogError("[LobbyManager] Fout bij spawnen van clone op index " + i + ": " + e.Message);
            }
        }

        clonesReady = true;
        Debug.Log($"[LobbyManager] Spawned {allPlayerClones.Count} player clones.");
    }

    private void ResetServerData()
    {
        allPlayerClones.Clear();
        cloneOccupied.Clear();
        clonesReady = false;
    }
}
