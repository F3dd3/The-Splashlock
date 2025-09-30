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
    public static LobbyManager Instance;

    public Button hostButton;
    public Button joinButton;
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI infoText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        hostButton.onClick.AddListener(HostGame);
        joinButton.onClick.AddListener(JoinGame);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (PlayerSpawner.Instance == null)
        {
            Debug.LogError("PlayerSpawner niet gevonden!");
            return;
        }

        // Spawn joiners server-side (host is al gespawned)
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            PlayerSpawner.Instance.SpawnPlayerServer(clientId);
        }
    }

    // Ontvang meldingen van PlayerSpawner
    public void ReceiveSpawnMessage(string playerLabel)
    {
        infoText.text = $"{playerLabel} heeft het spel joined!";
        Debug.Log($"{playerLabel} heeft het spel joined!");
    }

    // ================= Host =================
    private async void HostGame()
    {
        infoText.text = "Hosting game...";
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            byte[] allocationIdBytes = allocation.AllocationId.ToByteArray();
            byte[] keyBytes = allocation.Key;
            byte[] connectionData = allocation.ConnectionData;

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocationIdBytes,
                keyBytes,
                connectionData
            );

            NetworkManager.Singleton.StartHost();

            // Spawn host server-side
            if (PlayerSpawner.Instance != null && NetworkManager.Singleton.IsServer)
            {
                PlayerSpawner.Instance.SpawnPlayerServer(NetworkManager.Singleton.LocalClientId);
            }

            infoText.text = $"Game hosted! Join code: {joinCode}";
        }
        catch (Exception e)
        {
            Debug.LogError("Host mislukt: " + e.Message);
            infoText.text = "Host mislukt!";
        }
    }

    // ================= Join =================
    private async void JoinGame()
    {
        string joinCode = joinCodeInput.text.Trim();
        if (string.IsNullOrEmpty(joinCode))
        {
            infoText.text = "Voer een geldige join code in!";
            return;
        }

        infoText.text = "Joinen...";
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            byte[] allocationIdBytes = joinAllocation.AllocationId.ToByteArray();
            byte[] keyBytes = joinAllocation.Key;
            byte[] connectionData = joinAllocation.ConnectionData;

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
        }
        catch (Exception e)
        {
            Debug.LogError("Join mislukt: " + e.Message);
            infoText.text = "Join mislukt!";
        }
    }
}
