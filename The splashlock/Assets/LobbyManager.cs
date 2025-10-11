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

public class LobbyManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI infoText;
    public Button leaveLobbyButton;

    private bool servicesInitialized = false;
    private string lastJoinCode = "";
    private bool isAutoHost = false;

    private async void Awake()
    {
        await InitializeUnityServicesSafe();
    }

    private async Task InitializeUnityServicesSafe()
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
        leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);
        leaveLobbyButton.gameObject.SetActive(false);

        WaitUntilReadyAndAutoHost();
    }

    private void Update()
    {
        // Laat Leave Lobby knop alleen zien als er meerdere spelers in dezelfde server zijn
        bool multiplePlayers = NetworkManager.Singleton.ConnectedClientsList.Count > 1;
        leaveLobbyButton.gameObject.SetActive(multiplePlayers);
    }

    private async void WaitUntilReadyAndAutoHost()
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
            catch
            {
                await Task.Yield();
            }
            await Task.Yield();
        }

        isAutoHost = true;
        AutoHostGame();
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
        if (!servicesInitialized)
            return;

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
            try
            {
                var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
                if (localPlayer != null)
                    Destroy(localPlayer.gameObject);

                NetworkManager.Singleton.Shutdown();
                await Task.Delay(500);
            }
            catch { }
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
        }
        catch
        {
            infoText.text = "Invalid code!";
        }
    }

    private void OnLeaveLobbyClicked()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId == NetworkManager.Singleton.LocalClientId) continue;
                client.PlayerObject?.Despawn(true);
            }

            NetworkManager.Singleton.Shutdown();
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (localPlayer != null)
                Destroy(localPlayer.gameObject);

            NetworkManager.Singleton.Shutdown();
        }

        infoText.text = "";
        WaitUntilReadyAndAutoHost();
    }

    private void OnDestroy()
    {
        hostButton.onClick.RemoveAllListeners();
        joinButton.onClick.RemoveAllListeners();
        leaveLobbyButton.onClick.RemoveAllListeners();
    }
}
