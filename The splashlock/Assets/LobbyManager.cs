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

public class LobbyManager : MonoBehaviour
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
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        WaitUntilReadyAndAutoHost();
    }

    private async void WaitUntilReadyAndAutoHost()
    {
        await WaitForServicesReady();
        isAutoHost = true;
        AutoHostGame();
    }

    private async Task WaitForServicesReady()
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

        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            // Cleanup lokale player
            PlayerSpawner.Instance.ClientLeave(NetworkManager.Singleton.LocalClientId);
            Back.Instance?.ResetReadyStatus();

            // Disconnect client en wacht daarna auto-host
            NetworkManager.Singleton.OnClientDisconnectCallback += OnLocalClientDisconnected;
            NetworkManager.Singleton.Shutdown();
        }
        else if (NetworkManager.Singleton.IsHost)
        {
            // Cleanup alle clients
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList.ToList())
            {
                PlayerSpawner.Instance.RemovePlayer(client.ClientId);
            }
            Back.Instance?.ResetReadyStatus();

            // Disconnect host en wacht daarna auto-host
            NetworkManager.Singleton.OnClientDisconnectCallback += OnAllClientsDisconnected;
            NetworkManager.Singleton.Shutdown();
        }

        ResetLobbyState();
    }

    // Callback voor client leave / host-leave
    private void OnLocalClientDisconnected(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnLocalClientDisconnected;

        // Start auto-host op eigen server
        _ = AutoHostAfterDelay();
    }

    private void OnAllClientsDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 0)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnAllClientsDisconnected;

            // Start auto-host
            _ = AutoHostAfterDelay();
        }
    }

    // Detecteer host-leave terwijl je client bent
    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost) return;

        // Check of host weg is (host is meestal clientId=0)
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(0) ||
            NetworkManager.Singleton.ConnectedClientsList.Count == 1)
        {
            Debug.Log("[LobbyManager] Host disconnected, switching to auto-host...");

            // Cleanup lokale player
            PlayerSpawner.Instance.ClientLeave(NetworkManager.Singleton.LocalClientId);
            Back.Instance?.ResetReadyStatus();

            // Disconnect van oude server
            NetworkManager.Singleton.OnClientDisconnectCallback += OnLocalClientDisconnected;
            NetworkManager.Singleton.Shutdown();
        }
    }

    private async Task AutoHostAfterDelay()
    {
        // Wacht Relay / Transport cleanup
        await Task.Delay(1000);

        // Wacht tot Unity Services ready zijn
        await WaitForServicesReady();

        // Start nieuwe host
        AutoHostGame();
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
