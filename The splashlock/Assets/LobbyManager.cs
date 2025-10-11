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

        // Auto-host bij opstarten
        WaitUntilReadyAndAutoHost();
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
            catch (Exception e)
            {
                Debug.LogWarning("Sign-in failed, retrying: " + e.Message);
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

            PlayerSpawner.Instance.SpawnPlayer(NetworkManager.Singleton.LocalClientId);

            // Auto-host → geen infoText
            Debug.Log($"[AutoHost] Game hosted with code: {joinCode}");
        }
        catch (Exception e)
        {
            Debug.LogError("Auto-host failed: " + e.Message);
        }
    }

    private void OnHostButtonClicked()
    {
        // Alleen tonen van code bij handmatig hosten
        if (NetworkManager.Singleton.IsHost && !string.IsNullOrEmpty(lastJoinCode))
        {
            isAutoHost = false; // markeer als handmatig
            infoText.text = lastJoinCode;
            Debug.Log($"[ManualHost] Showing join code: {lastJoinCode}");
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
        catch (RelayServiceException ex)
        {
            Debug.LogWarning("Invalid room code: " + ex.Message);
            infoText.text = "Invalid code!";
            return;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to join: " + ex.Message);
            infoText.text = "Invalid code!";
            return;
        }

        // Alleen unhost/local player verwijderen bij geldige join
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
            catch (Exception ex)
            {
                Debug.LogWarning("Error cleaning host before joining: " + ex.Message);
            }
        }

        // Client starten
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

            // ✅ Succesvol join → infoText laat zien met roomcode
            infoText.text = $"Connected to: {joinCode}";
            Debug.Log($"Joined as CLIENT with join code: {joinCode}");

            await Task.Delay(300);
            PlayerBroadcaster.Instance?.ShowLocalJoinMessage("You joined as client!");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to join client: " + e.Message);
            infoText.text = "Invalid code!";
        }
    }

    private void OnDestroy()
    {
        hostButton.onClick.RemoveAllListeners();
        joinButton.onClick.RemoveAllListeners();
    }
}
