using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class Back : NetworkBehaviour
{
    public static Back Instance;

    public Button readyButton;
    public TextMeshProUGUI readyStatusText;

    private List<ulong> readyClients = new List<ulong>();
    private bool isLocalReady = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        readyButton.onClick.AddListener(OnReadyClicked);
        UpdateButtonText();

        // ✅ Ready knop is altijd zichtbaar
        readyButton.gameObject.SetActive(true);

        // ✅ Stats alleen zichtbaar bij meerdere spelers
        SetReadyStatsVisible(false);

        // ✅ Direct bij start updaten (ook bij 1 speler)
        InvokeRepeating(nameof(RefreshReadyStatusUI), 0.5f, 1.0f);
    }

    private void OnDestroy()
    {
        readyButton.onClick.RemoveListener(OnReadyClicked);
    }

    private void OnReadyClicked()
    {
        if (!IsClient) return;

        isLocalReady = !isLocalReady;
        UpdateButtonText();

        if (isLocalReady)
            SetReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        else
            UnsetReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    private void UpdateButtonText()
    {
        if (readyButton != null)
            readyButton.GetComponentInChildren<TextMeshProUGUI>().text = isLocalReady ? "Cancel Ready" : "Ready";
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyServerRpc(ulong clientId)
    {
        if (!readyClients.Contains(clientId))
            readyClients.Add(clientId);

        UpdateReadyStatusClientRpc(readyClients.ToArray());
        CheckAllReady();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnsetReadyServerRpc(ulong clientId)
    {
        if (readyClients.Contains(clientId))
            readyClients.Remove(clientId);

        UpdateReadyStatusClientRpc(readyClients.ToArray());
    }

    [ClientRpc]
    private void UpdateReadyStatusClientRpc(ulong[] readyIds)
    {
        readyClients = new List<ulong>(readyIds);
        RefreshReadyStatusUI();
    }

    // ✅ Nieuwe methode die altijd de actuele ready-status toont
    private void RefreshReadyStatusUI()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.ConnectedClientsList == null)
            return;

        int playerCount = NetworkManager.Singleton.ConnectedClientsList.Count;
        bool multiplePlayers = playerCount > 1;

        SetReadyStatsVisible(multiplePlayers);

        if (!multiplePlayers)
        {
            readyStatusText.text = "";
            return;
        }

        string status = "Ready Players:\n";
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            bool isReady = readyClients.Contains(client.ClientId);
            status += $"Player {client.ClientId}: {(isReady ? "✅ Ready" : "❌ Not Ready")}\n";
        }

        readyStatusText.text = status;
    }

    private void CheckAllReady()
    {
        int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;
        if (readyClients.Count == totalPlayers && totalPlayers > 0)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    public void RemoveClientReadyStatus(ulong clientId)
    {
        if (readyClients.Contains(clientId))
            readyClients.Remove(clientId);

        UpdateReadyStatusClientRpc(readyClients.ToArray());
    }

    // ✅ Alleen de tekst van de status (lijst) verbergen, knop blijft zichtbaar
    public void SetReadyStatsVisible(bool visible)
    {
        if (readyStatusText != null)
        {
            readyStatusText.gameObject.SetActive(visible);
            if (!visible)
                readyStatusText.text = "";
        }
    }
}
