using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI infoText;

    private void Start()
    {
        hostButton.onClick.AddListener(HostGame);
        joinButton.onClick.AddListener(JoinGame);
    }

    // ================= Host =================
    private async void HostGame()
    {
        if (!UnityServicesInitializer.ServicesInitialized)
        {
            infoText.text = "Services not initialized!";
            return;
        }

        infoText.text = "Hosting game...";
        try
        {
            // Maak een Relay allocation aan (max 4 spelers)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);

            // Haal de join code op
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Zet GUID om naar byte[] voor UnityTransport
            byte[] allocationIdBytes = allocation.AllocationId.ToByteArray();
            byte[] keyBytes = allocation.Key;
            byte[] connectionData = allocation.ConnectionData;

            // Stel UnityTransport in
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocationIdBytes,
                keyBytes,
                connectionData
            );

            NetworkManager.Singleton.StartHost();
            infoText.text = "Game hosted!\nJoin code: " + joinCode;
            Debug.Log("Game hosted! Join code: " + joinCode);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to host game: " + e.Message);
            infoText.text = "Host failed!";
        }
    }

    // ================= Join =================
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
        if (!UnityServicesInitializer.ServicesInitialized)
        {
            infoText.text = "Services not initialized!";
            return;
        }

        try
        {
            // Join een bestaande Relay allocation
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Zet GUID om naar byte[] voor UnityTransport
            byte[] allocationIdBytes = joinAllocation.AllocationId.ToByteArray();
            byte[] keyBytes = joinAllocation.Key;
            byte[] connectionData = joinAllocation.ConnectionData;

            // Stel UnityTransport in
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                allocationIdBytes,
                keyBytes,
                connectionData
            );

            NetworkManager.Singleton.StartClient();
            infoText.text = "Joined game!";
            Debug.Log("Joined game with code: " + joinCode);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to join game: " + e.Message);
            infoText.text = "Join failed!";
        }
    }
}
