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
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    [Header("UI Elements")]
    public Button joinButton;
    public Button leaveButton;
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI infoText;

    [Header("Player Prefab & Spawn Points")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Player Colors")]
    public List<Color> allColors = new List<Color>
        { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

    // Server-side tracking
    private Dictionary<ulong, int> clientSpawnIndices = new Dictionary<ulong, int>();
    private Dictionary<ulong, Color> clientColors = new Dictionary<ulong, Color>();
    private int nextSpawnIndex = 1; // host = 0
    private int nextColorIndex = 1;

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
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

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
            ResetServerData();

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
        JoinAllocation allocation;
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

        infoText.text = $"Connected to: {joinCode}";
        leaveButton.gameObject.SetActive(false);
        hasJoinedOnce = true;
    }

    private async Task WaitUntilClientConnectedAsync()
    {
        while (NetworkManager.Singleton == null ||
               !NetworkManager.Singleton.IsClient ||
               NetworkManager.Singleton.LocalClient == null)
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
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList.ToList())
        {
            if (client.ClientId != NetworkManager.Singleton.LocalClientId)
                NetworkManager.Singleton.DisconnectClient(client.ClientId);
        }

        ResetServerData();
        leaveButton.gameObject.SetActive(false);
        infoText.text = $"Join code: {lastJoinCode}";

        NetworkManager.Singleton.Shutdown();

        while (NetworkManager.Singleton.IsListening)
            await Task.Yield();

        await WaitForServicesReadyAsync();
        AutoHostGame();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost && !autoHostPending)
        {
            autoHostPending = true;
            _ = ClientAutohostFlowAsync();
        }

        clientSpawnIndices.Remove(clientId);
        clientColors.Remove(clientId);
    }

    private async Task ClientAutohostFlowAsync()
    {
        ResetServerData();
        infoText.text = "Host left, starting own server...";

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            NetworkManager.Singleton.Shutdown();

        await WaitForServicesReadyAsync();
        AutoHostGame();

        autoHostPending = false;
    }

    // ---------------- Server-side player spawning ----------------
    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            SpawnPlayerServer(clientId);
        }
    }

    private void SpawnPlayerServer(ulong clientId)
    {
        if (playerPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("PlayerPrefab of SpawnPoints niet ingesteld!");
            return;
        }

        int spawnIndex;
        Color color;

        if (!clientSpawnIndices.ContainsKey(clientId))
        {
            spawnIndex = (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsHost) ? 0 : Mathf.Min(nextSpawnIndex, spawnPoints.Length - 1);
            color = (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsHost) ? allColors[0] : allColors[nextColorIndex % allColors.Count];

            clientSpawnIndices[clientId] = spawnIndex;
            clientColors[clientId] = color;

            if (clientId != NetworkManager.Singleton.LocalClientId || !NetworkManager.Singleton.IsHost)
            {
                nextSpawnIndex++;
                nextColorIndex++;
            }
        }
        else
        {
            spawnIndex = clientSpawnIndices[clientId];
            color = clientColors[clientId];
        }

        // Correct positie vóór spawn instellen
        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        Quaternion spawnRot = Quaternion.Euler(0, 180, 0);

        GameObject playerObj = Instantiate(playerPrefab, spawnPos, spawnRot);
        NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, true);

        // Zet kleur via NetworkVariable
        Player playerScript = playerObj.GetComponent<Player>();
        Vector3 colorVec = new Vector3(color.r, color.g, color.b);
        playerScript.SetColorServerRpc(colorVec);

        if (clientId == NetworkManager.Singleton.LocalClientId && playerScript.nameLabel != null)
            playerScript.nameLabel.text = "You";
    }

    private void ResetServerData()
    {
        clientSpawnIndices.Clear();
        clientColors.Clear();
        nextSpawnIndex = 1;
        nextColorIndex = 1;
    }
}
