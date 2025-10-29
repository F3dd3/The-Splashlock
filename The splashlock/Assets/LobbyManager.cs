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
    private bool hasJoinedOnce = false;
    private bool autoHostPending = false;
    private bool isShowingInvalidCode = false;

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
        infoText.gameObject.SetActive(true);

        joinButton.onClick.AddListener(() => _ = JoinGameAsync());

        leaveButton.gameObject.SetActive(false);
        leaveButton.onClick.AddListener(() =>
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                _ = HostLeaveFlowAsync();
        });

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

            // Host spawnt zichzelf
            PlayerSpawner.Instance?.SpawnPlayer(NetworkManager.Singleton.LocalClientId, true);

            infoText.text = $"Join code: {lastJoinCode}";
            leaveButton.gameObject.SetActive(false);
            hasJoinedOnce = true;
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

        infoText.text = "Trying to connect...";

        if (string.IsNullOrEmpty(joinCode))
        {
            await ShowInvalidCodeTemporarily();
            return;
        }

        await WaitForServicesReadyAsync();
        await JoinRelayAsync(joinCode);
    }

    private async Task JoinRelayAsync(string joinCode)
    {
        if (!servicesInitialized) return;

        infoText.text = "Connecting...";

        JoinAllocation allocation = null;

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

            await WaitUntilClientConnectedAsync();

            // ✅ Client spawn niet zelf, server doet dat
            infoText.text = $"Connected to: {joinCode}";
            leaveButton.gameObject.SetActive(false);
            hasJoinedOnce = true;
        }
        catch
        {
            await ShowInvalidCodeTemporarily();
        }
    }

    private async Task WaitUntilClientConnectedAsync()
    {
        while (NetworkManager.Singleton == null ||
               !NetworkManager.Singleton.IsClient ||
               NetworkManager.Singleton.LocalClient == null ||
               NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            await Task.Yield();
        }
    }

    private async Task ShowInvalidCodeTemporarily()
    {
        if (isShowingInvalidCode) return;
        isShowingInvalidCode = true;

        string originalCode = lastJoinCode;
        infoText.text = "Invalid code!";
        float timer = 0f;
        float duration = 2f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            await Task.Yield();

            if (!string.IsNullOrEmpty(infoText.text) && infoText.text.StartsWith("Connected"))
                break;
        }

        infoText.text = $"Join code: {originalCode}";
        isShowingInvalidCode = false;
    }

    public void SetLeaveButtonVisible(bool visible)
    {
        if (leaveButton != null)
            leaveButton.gameObject.SetActive(visible && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost);
    }

    private async Task HostLeaveFlowAsync()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        PlayerSpawner.Instance?.ResetAll();
        Back.Instance?.ResetReadyStatus();

        NetworkManager.Singleton.Shutdown();

        await Task.Delay(300);
        AutoHostGame();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        PlayerSpawner.Instance?.RemovePlayer(clientId);
    }
}
