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

    private List<GameObject> allPlayerClones = new List<GameObject>();
    private List<bool> cloneOccupied = new List<bool>();

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
            foreach (var clone in allPlayerClones)
            {
                if (clone != null)
                    Destroy(clone);
            }
            allPlayerClones.Clear();

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
        if (!NetworkManager.Singleton.IsServer) return;

        GameObject clone = allPlayerClones.FirstOrDefault(c => c.activeSelf && c.GetComponent<Player>().ownerClientId.Value == clientId);
        if (clone != null)
            clone.GetComponent<Player>().isVisible.Value = false;

        int index = allPlayerClones.FindIndex(c => c == clone);
        if (index != -1)
            cloneOccupied[index] = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        int nextIndex = cloneOccupied.FindIndex(o => o == false);
        if (nextIndex == -1)
        {
            Debug.LogWarning("Geen vrije player clone beschikbaar voor client: " + clientId);
            return;
        }

        GameObject clone = allPlayerClones[nextIndex];
        Player playerScript = clone.GetComponent<Player>();

        // Clone toewijzen
        playerScript.ownerClientId.Value = clientId;
        playerScript.isVisible.Value = true;
        cloneOccupied[nextIndex] = true;

        // Maak client eigenaar van het NetworkObject
        clone.GetComponent<NetworkObject>().ChangeOwnership(clientId);

        Debug.Log($"Clone {nextIndex} toegewezen aan client {clientId}");

        // Optioneel: volgende clone voorbereiden
        PrepareNextClone();
    }

    private void PrepareNextClone()
    {
        int nextIndex = cloneOccupied.FindIndex(o => o == false);
        if (nextIndex != -1)
        {
            GameObject nextClone = allPlayerClones[nextIndex];
            Player playerScript = nextClone.GetComponent<Player>();

            // Kan zichtbaar of invisible zijn als “waiting”
            playerScript.isVisible.Value = false;
            // playerScript.nameLabel.text = "Waiting..."; // optioneel
        }
    }

    private void SpawnAllPlayerClones()
    {
        cloneOccupied.Clear();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject playerObj = Instantiate(playerPrefab, spawnPoints[i].position, Quaternion.Euler(0, 180, 0));
            Player playerScript = playerObj.GetComponent<Player>();

            bool isVisible = (i == 0);
            playerScript.isVisible.Value = isVisible;
            playerScript.isHostPlayer.Value = (i == 0);

            // Host eigen clone krijgt LocalClientId
            playerScript.ownerClientId.Value = (i == 0) ? NetworkManager.Singleton.LocalClientId : 0;

            NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            Color color = allColors[i % allColors.Count];
            playerScript.SetColorServerRpc(new Vector3(color.r, color.g, color.b));

            allPlayerClones.Add(playerObj);
            cloneOccupied.Add(isVisible);
        }

        // Bereid meteen de eerste vrije clone voor
        PrepareNextClone();
    }

    private void ResetServerData()
    {
        allPlayerClones.Clear();
        cloneOccupied.Clear();
    }
}
