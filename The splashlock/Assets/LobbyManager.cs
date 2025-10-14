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

public class LobbyManager : NetworkBehaviour
{
    [Header("UI Elements")]
    public Button hostButton;
    public Button joinButton;
    public Button leaveButton;
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI infoText;

    private bool servicesInitialized = false;
    private string lastJoinCode = "";
    private bool isAutoHost = false;
    private bool autoHostPending = false;

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
        hostButton.onClick.AddListener(OnHostButtonClicked);
        joinButton.onClick.AddListener(JoinGame);
        leaveButton.onClick.AddListener(LeaveLobby);
        leaveButton.gameObject.SetActive(false);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        WaitUntilReadyAndAutoHost();
    }

    private async void WaitUntilReadyAndAutoHost()
    {
        await WaitForServicesReadyAsync();
        isAutoHost = true;
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
            PlayerSpawner.Instance.SpawnPlayer(NetworkManager.Singleton.LocalClientId);
            Debug.Log($"[AutoHost] Game hosted with code: {joinCode}");
        }
        catch (Exception e)
        {
            Debug.LogError("Auto-host failed: " + e.Message);
        }
    }

    private void OnHostButtonClicked()
    {
        if (NetworkManager.Singleton.IsHost && !string.IsNullOrEmpty(lastJoinCode))
        {
            isAutoHost = false;
            infoText.text = lastJoinCode;
            leaveButton.gameObject.SetActive(true);
        }
    }

    private void JoinGame()
    {
        string joinCode = joinCodeInput.text.Trim();
        if (string.IsNullOrEmpty(joinCode))
        {
            infoText.text = "Invalid code!";
            return;
        }
        JoinRelay(joinCode);
    }

    private async void JoinRelay(string joinCode)
    {
        if (!servicesInitialized) return;

        JoinAllocation allocation = null;
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch
        {
            infoText.text = "Invalid code!";
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (localPlayer != null) Destroy(localPlayer.gameObject);
            NetworkManager.Singleton.Shutdown();
            await Task.Delay(500);
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
            infoText.text = $"Connected to: {joinCode}";
            leaveButton.gameObject.SetActive(true);
        }
        catch
        {
            infoText.text = "Invalid code!";
        }
    }

    public void LeaveLobby()
    {
        if (NetworkManager.Singleton == null) return;

        leaveButton.gameObject.SetActive(false);

        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[LobbyManager] Host leaving, notifying clients...");
            NotifyClientsToLeaveClientRpc();
            _ = HostLeaveFlowAsync();
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            _ = ClientLeaveFlowAsync();
        }
    }

    [ClientRpc]
    private void NotifyClientsToLeaveClientRpc(ClientRpcParams rpcParams = default)
    {
        if (IsHost) return;
        Debug.Log("[LobbyManager] Host leave received, client leaving & auto-hosting...");
        _ = ClientLeaveFlowAsync();
    }

    private async Task ClientLeaveFlowAsync()
    {
        if (autoHostPending) return;
        autoHostPending = true;

        // Cleanup lokaal
        PlayerSpawner.Instance.ClientLeave(NetworkManager.Singleton.LocalClientId);
        Back.Instance?.ResetReadyStatus();
        ResetLobbyState();

        // Disconnect client
        NetworkManager.Singleton.Shutdown();

        // Wacht tot volledig losgekoppeld
        await Task.Yield();
        while (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            await Task.Yield();

        // Wacht tot services klaar zijn
        await WaitForServicesReadyAsync();

        // Auto-host
        AutoHostGame();
        autoHostPending = false;
    }

    private async Task HostLeaveFlowAsync()
    {
        // Cleanup alle clients
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList.ToList())
            PlayerSpawner.Instance.RemovePlayer(client.ClientId);

        Back.Instance?.ResetReadyStatus();
        ResetLobbyState();

        // Wacht tot clients weg zijn
        await Task.Yield();
        while (NetworkManager.Singleton.ConnectedClients.Count > 1)
            await Task.Yield();

        Debug.Log("[LobbyManager] All clients left, shutting down host & auto-hosting...");

        NetworkManager.Singleton.Shutdown();

        // Wacht tot shutdown klaar
        await Task.Yield();
        while (NetworkManager.Singleton.IsListening)
            await Task.Yield();

        await WaitForServicesReadyAsync();

        AutoHostGame();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // fallback voor onverwachte host-leave
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.Log("[LobbyManager] Host lost unexpectedly, auto-hosting...");
            _ = ClientLeaveFlowAsync();
        }
    }

    private void ResetLobbyState()
    {
        PlayerSpawner.Instance.ResetSpawnPoints();
        Back.Instance?.ResetReadyStatus();
    }

    public void SetLeaveButtonVisible(bool visible)
    {
        if (leaveButton != null)
            leaveButton.gameObject.SetActive(visible);
    }
}
