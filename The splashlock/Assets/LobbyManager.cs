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
            // Init Unity Services
            await UnityServices.InitializeAsync();

            // Alleen aanmelden als niet ingelogd
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                try
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log("Signed in as: " + AuthenticationService.Instance.PlayerId);
                    infoText.text = "Signed in as: " + AuthenticationService.Instance.PlayerId;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Sign-in skipped or already in progress: " + e.Message);
                }
            }
            else
            {
                Debug.Log("Already signed in: " + AuthenticationService.Instance.PlayerId);
                infoText.text = "Already signed in: " + AuthenticationService.Instance.PlayerId;
            }

            servicesInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Services initialization failed: " + e.Message);
            infoText.text = "Services init failed!";
        }
    }

    private void Start()
    {
        hostButton.onClick.AddListener(HostGame);
        joinButton.onClick.AddListener(JoinGame);
    }

    private async void HostGame()
    {
        if (!servicesInitialized)
        {
            infoText.text = "Services not initialized!";
            return;
        }

        infoText.text = "Hosting game...";
        try
        {
            // Relay allocation
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
            infoText.text = $"Game hosted!\nJoin code: {joinCode}";
            Debug.Log($"Game hosted! Join code: {joinCode}");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to host game: " + e.Message);
            infoText.text = "Host failed!";
        }
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

    private async void JoinRelay(string joinCode)
    {
        if (!servicesInitialized)
        {
            infoText.text = "Services not initialized!";
            return;
        }

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
            infoText.text = "Joined game!";
            Debug.Log("Joined game with code: " + joinCode);

            // Lokale melding dat client host heeft gejoined
            if (PlayerBroadcaster.Instance != null)
                PlayerBroadcaster.Instance.ShowLocalJoinMessage("You joined the host!");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to join game: " + e.Message);
            infoText.text = "Join failed!";
        }
    }
}
