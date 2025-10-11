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
            infoText.text = "Unity Services ready!";
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Services init failed: " + e.Message);
            infoText.text = "Services init failed!";
        }
    }

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostButtonClicked);
        joinButton.onClick.AddListener(JoinGame);

        // ✅ Wacht tot alles klaar is voordat auto-host
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
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    infoText.text = "Signed in as: " + AuthenticationService.Instance.PlayerId;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Sign-in failed, retrying: " + e.Message);
            }

            await Task.Yield();
        }

        AutoHostGame();
    }

    private async void AutoHostGame()
    {
        infoText.text = "Auto-hosting game...";

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            infoText.text = $"Auto-hosted!\nJoin code: {joinCode}";

            PlayerSpawner.Instance.SpawnPlayer(NetworkManager.Singleton.LocalClientId);
        }
        catch (Exception e)
        {
            Debug.LogError("Auto-host failed: " + e.Message);
            infoText.text = "Auto-host failed!";
        }
    }

    private void OnHostButtonClicked()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        infoText.text = "You are hosting (server code active).";
    }

    private void JoinGame()
    {
        string joinCode = joinCodeInput.text.Trim();
        if (string.IsNullOrEmpty(joinCode))
        {
            infoText.text = "Enter a valid join code!";
            return;
        }

        infoText.text = "Joining game...";
        JoinRelay(joinCode);
    }

    // ✅ Belangrijk: nieuwe JoinRelay logica met auto-unhost
    private async void JoinRelay(string joinCode)
    {
        if (!servicesInitialized)
        {
            infoText.text = "Services not initialized!";
            return;
        }

        // 🧹 Als je zelf host bent, eerst netjes afsluiten
        if (NetworkManager.Singleton.IsHost)
        {
            infoText.text = "Closing your current host session...";

            try
            {
                // Verwijder lokale player als die nog bestaat
                var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
                if (localPlayer != null)
                {
                    Destroy(localPlayer.gameObject);
                }

                // Stop netwerk
                NetworkManager.Singleton.Shutdown();

                await Task.Delay(500); // kleine pauze zodat Unity kan opruimen
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Error cleaning host before joining: " + ex.Message);
            }
        }

        // 🕹️ Daarna pas joinen als client
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

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
            infoText.text = "✅ Connected as client!";
            Debug.Log("Now acting as CLIENT on host with join code: " + joinCode);

            await Task.Delay(300);
            PlayerBroadcaster.Instance?.ShowLocalJoinMessage("You joined as client!");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to join game: " + e.Message);
            infoText.text = "Join failed!";
        }
    }

    private void OnDestroy()
    {
        hostButton.onClick.RemoveAllListeners();
        joinButton.onClick.RemoveAllListeners();
    }
}
