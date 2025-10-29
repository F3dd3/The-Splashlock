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
    public GameObject playerPrefab; // sleep prefab hier in inspector
    public Transform[] spawnPoints;

    [Header("Player Colors")]
    public List<Color> allColors = new List<Color>
        { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

    // Server-side tracking
    private Dictionary<ulong, int> clientSpawnIndices = new Dictionary<ulong, int>();
    private Dictionary<ulong, Color> clientColors = new Dictionary<ulong, Color>();
    private Dictionary<ulong, GameObject> spawnedPlayers = new Dictionary<ulong, GameObject>();
    private int nextSpawnIndex = 1; // host = 0
    private int nextColorIndex = 1; // host = 0

    private bool servicesInitialized = false;
    private string lastJoinCode = "";
    private bool hasJoinedOnce = false;
    private bool autoHostPending = false;
    private bool isShowingInvalidCode = false;

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
            try { await AuthenticationService.Instance.SignInAnonymouslyAsync(); } catch { }
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

            SpawnPlayerServer(NetworkManager.Singleton.LocalClientId); // host

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
        try { allocation = await RelayService.Instance.JoinAllocationAsync(joinCode); }
        catch { await ShowInvalidCodeTemporarily(); return; }

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

        // spawn enkel server-side via OnClientConnectedCallback
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

        // free spawnpoint en kleur
        clientSpawnIndices.Remove(clientId);
        clientColors.Remove(clientId);

        if (spawnedPlayers.ContainsKey(clientId))
        {
            var netObj = spawnedPlayers[clientId].GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
            spawnedPlayers.Remove(clientId);
        }
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

    // ---------------- Server-side spawning ----------------
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            SpawnPlayerServer(clientId);
        };
    }

    private void SpawnPlayerServer(ulong clientId)
    {
        if (!IsServer || playerPrefab == null || spawnPoints.Length == 0) return;
        if (spawnedPlayers.ContainsKey(clientId)) return; // al gespawned

        int spawnIndex;
        Color color;

        if (!clientSpawnIndices.ContainsKey(clientId))
        {
            if (IsHost && clientId == NetworkManager.Singleton.LocalClientId)
            {
                spawnIndex = 0;
                color = allColors[0];
            }
            else
            {
                spawnIndex = Mathf.Min(nextSpawnIndex, spawnPoints.Length - 1);
                color = allColors[nextColorIndex % allColors.Count];
                nextSpawnIndex++;
                nextColorIndex++;
            }

            clientSpawnIndices[clientId] = spawnIndex;
            clientColors[clientId] = color;
        }
        else
        {
            spawnIndex = clientSpawnIndices[clientId];
            color = clientColors[clientId];
        }

        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
        var netObj = playerObj.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.SpawnWithOwnership(clientId);

        // kleur server -> client
        Player playerScript = playerObj.GetComponent<Player>();
        playerScript.SetColorServerRpc(new Vector3(color.r, color.g, color.b));

        spawnedPlayers[clientId] = playerObj;
    }

    private void ResetServerData()
    {
        clientSpawnIndices.Clear();
        clientColors.Clear();
        spawnedPlayers.Clear();
        nextSpawnIndex = 1;
        nextColorIndex = 1;
    }
}
