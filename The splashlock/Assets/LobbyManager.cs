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
    public Button joinButton;
    public Button leaveButton;
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI infoText;

    private bool servicesInitialized = false;
    private string lastJoinCode = "";
    private bool autoHostPending = false;
    private bool hasJoinedOnce = false;

    private void Awake() => InitializeUnityServicesSafe();

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

        joinButton.onClick.AddListener(() =>
        {
            JoinGameAsync();
        });

        leaveButton.gameObject.SetActive(false);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        WaitUntilReadyAndAutoHost();
    }

    private async void WaitUntilReadyAndAutoHost()
    {
        await WaitForServicesReadyAsync();
        if (!hasJoinedOnce)
            AutoHostGame();
    }

    private async Task WaitForServicesReadyAsync()
    {
        while (!servicesInitialized) await Task.Yield();

        while (!AuthenticationService.Instance.IsSignedIn || string.IsNullOrEmpty(AuthenticationService.Instance.PlayerId))
        {
            try { await AuthenticationService.Instance.SignInAnonymouslyAsync(); } catch { }
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
            PlayerSpawner.Instance?.SpawnPlayer(NetworkManager.Singleton.LocalClientId, true);

            infoText.gameObject.SetActive(true);
            infoText.text = $"Join code: {lastJoinCode}";

            leaveButton.gameObject.SetActive(false); // autohost: knop uit

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

        JoinAllocation allocation;
        try { allocation = await RelayService.Instance.JoinAllocationAsync(joinCode); }
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

            PlayerSpawner.Instance?.ClientLeave(NetworkManager.Singleton.LocalClientId);
            await Task.Delay(100);
            PlayerSpawner.Instance?.SpawnPlayer(NetworkManager.Singleton.LocalClientId, true);

            infoText.gameObject.SetActive(true);
            infoText.text = $"Connected to: {joinCode}";

            leaveButton.gameObject.SetActive(false); // client: nooit zichtbaar
            hasJoinedOnce = true;
        }
        catch
        {
            infoText.gameObject.SetActive(true);
            infoText.text = "Invalid code!";
        }
    }

    public void LeaveLobby()
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsHost) return; // alleen host mag leave

        _ = HostLeaveFlowAsync();
    }

    private async Task HostLeaveFlowAsync()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList.ToList())
        {
            if (client.ClientId != NetworkManager.Singleton.LocalClientId)
                PlayerSpawner.Instance?.RemovePlayer(client.ClientId);
        }

        Back.Instance?.ResetReadyStatus();
        PlayerSpawner.Instance?.ResetAll();

        leaveButton.gameObject.SetActive(false);
        infoText.gameObject.SetActive(false);

        NetworkManager.Singleton.Shutdown();
        while (NetworkManager.Singleton.IsListening) await Task.Yield();

        await WaitForServicesReadyAsync();
        AutoHostGame();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Clients doen niks bij disconnect
    }

    public void SetLeaveButtonVisible(bool visible)
    {
        if (leaveButton != null)
            leaveButton.gameObject.SetActive(visible && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost);
    }
}
