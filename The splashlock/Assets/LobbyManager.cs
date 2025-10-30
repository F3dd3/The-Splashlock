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
    private List<GameObject> allPlayerClones = new List<GameObject>();

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

            // ✅ Spawn alle player clones direct bij autohost
            if (NetworkManager.Singleton.IsServer)
                SpawnAllPlayerClones();
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

        // Maak de clone van de disconnected client weer onzichtbaar
        GameObject clone = allPlayerClones.FirstOrDefault(c => c.GetComponent<NetworkObject>().OwnerClientId == clientId);
        if (clone != null)
            clone.SetActive(false);
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
        if (!NetworkManager.Singleton.IsServer) return;

        // Vind eerste onzichtbare clone
        GameObject cloneToUse = allPlayerClones.FirstOrDefault(c => !c.activeSelf);
        if (cloneToUse != null)
        {
            cloneToUse.SetActive(true);
            NetworkObject netObj = cloneToUse.GetComponent<NetworkObject>();
            netObj.ChangeOwnership(clientId);

            Player playerScript = cloneToUse.GetComponent<Player>();
            if (playerScript.nameLabel != null)
                playerScript.nameLabel.text = "You";
        }
        else
        {
            Debug.LogWarning("Geen beschikbare player clones over!");
        }
    }

    private void SpawnAllPlayerClones()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject playerObj = Instantiate(playerPrefab, spawnPoints[i].position, Quaternion.Euler(0, 180, 0));
            NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            Player playerScript = playerObj.GetComponent<Player>();
            Color color = allColors[i % allColors.Count];
            playerScript.SetColorServerRpc(new Vector3(color.r, color.g, color.b));

            bool isVisible = (i == 0); // Alleen host zichtbaar
            playerObj.SetActive(isVisible);

            allPlayerClones.Add(playerObj);
        }
    }

    private void ResetServerData()
    {
        allPlayerClones.Clear();
    }
}
