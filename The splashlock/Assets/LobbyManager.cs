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

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("UI Elements")]
    public Button joinButton;
    public Button leaveButton; // Back to Lobby
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI infoText;

    private bool servicesInitialized = false;
    private string lastJoinCode = "";
    private bool hasJoinedOnce = false;
    private bool autoHostPending = false;
    private bool isReturningFromGame = false;

    public NetworkVariable<ulong> HostClientId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

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
        infoText.gameObject.SetActive(false);

        joinButton.onClick.AddListener(() => _ = JoinGameAsync());

        leaveButton.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                ReturnToLobbyFromGame();
            }
        });

        leaveButton.gameObject.SetActive(false);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        WaitUntilReadyAndAutoHost();
    }

    private async void WaitUntilReadyAndAutoHost()
    {
        await WaitForServicesReadyAsync();
        if (!hasJoinedOnce && !isReturningFromGame)
            AutoHostGame();
    }

    private async Task WaitForServicesReadyAsync()
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

    private async void AutoHostGame()
    {
        try
        {
            PlayerSpawner.Instance?.ResetAll();
            Back.Instance?.ResetReadyStatus();

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

            HostClientId.Value = NetworkManager.Singleton.LocalClientId; // Host bijhouden

            PlayerSpawner.Instance?.SpawnPlayer(NetworkManager.Singleton.LocalClientId, true);

            infoText.gameObject.SetActive(true);
            infoText.text = $"Join code: {lastJoinCode}";
            leaveButton.gameObject.SetActive(true);

            hasJoinedOnce = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Auto-host failed: " + e.Message);
            infoText.gameObject.SetActive(true);
            infoText.text = "Auto-host failed!";
        }
    }

    private async Task JoinGameAsync()
    {
        string joinCode = joinCodeInput.text.Trim();
        if (string.IsNullOrEmpty(joinCode))
        {
            infoText.gameObject.SetActive(true);
            infoText.text = "Invalid code!";
            return;
        }

        await WaitForServicesReadyAsync();
        await JoinRelayAsync(joinCode);
    }

    private async Task JoinRelayAsync(string joinCode)
    {
        if (!servicesInitialized) return;

        JoinAllocation allocation = null;
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch
        {
            infoText.gameObject.SetActive(true);
            infoText.text = "Invalid code!";
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (localPlayer != null) Destroy(localPlayer.gameObject);
            NetworkManager.Singleton.Shutdown();
            await Task.Delay(300);
        }

        try
        {
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

            PlayerSpawner.Instance?.SpawnPlayer(NetworkManager.Singleton.LocalClientId, true);

            infoText.gameObject.SetActive(true);
            infoText.text = $"Connected to: {joinCode}";
            leaveButton.gameObject.SetActive(false);

            hasJoinedOnce = true;
        }
        catch
        {
            infoText.gameObject.SetActive(true);
            infoText.text = "Invalid code!";
        }
    }

    // 🔹 Host-only Back to Lobby
    public void ReturnToLobbyFromGame()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        isReturningFromGame = true;

        Back.Instance?.ResetReadyStatus();
        PlayerSpawner.Instance?.ResetForLobby();

        NetworkManager.Singleton.SceneManager.LoadScene("MainLobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLobbySceneLoaded;
    }

    private void OnLobbySceneLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadMode, List<ulong> completedClients, List<ulong> timedOutClients)
    {
        if (sceneName != "MainLobby") return;

        // Host spawn
        PlayerSpawner.Instance?.SpawnPlayer(NetworkManager.Singleton.LocalClientId, true);

        // Stuur clients terug via RPC
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != NetworkManager.Singleton.LocalClientId)
                SendClientBackToLobbyServerRpc(client.ClientId);
        }

        Back.Instance?.ResetReadyStatus();
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLobbySceneLoaded;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendClientBackToLobbyServerRpc(ulong clientId)
    {
        SendClientBackToLobbyClientRpc(clientId);
    }

    [ClientRpc]
    private void SendClientBackToLobbyClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        NetworkManager.Singleton.SceneManager.LoadScene("MainLobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
        PlayerSpawner.Instance?.SpawnPlayer(NetworkManager.Singleton.LocalClientId, true);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost && !autoHostPending)
        {
            autoHostPending = true;
            _ = ClientAutohostFlowAsync();
        }
    }

    private async Task ClientAutohostFlowAsync()
    {
        PlayerSpawner.Instance?.ResetAll();
        Back.Instance?.ResetReadyStatus();

        infoText.gameObject.SetActive(true);
        infoText.text = "Host left, starting own server...";

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            NetworkManager.Singleton.Shutdown();

        await WaitForServicesReadyAsync();
        AutoHostGame();

        autoHostPending = false;
    }
}
