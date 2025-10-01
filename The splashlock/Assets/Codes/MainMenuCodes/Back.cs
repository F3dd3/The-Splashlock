using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class Back : NetworkBehaviour
{
    [Header("UI Elements")]
    public Button readyButton;
    public TextMeshProUGUI readyStatusText;

    // Server-side lijst van ready clients
    private List<ulong> readyClients = new List<ulong>();

    // Bijhouden of lokale speler ready is
    private bool isLocalReady = false;

    private void Start()
    {
        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);

        UpdateReadyStatusUI();
        UpdateButtonText();
    }

    private void OnDestroy()
    {
        if (readyButton != null)
            readyButton.onClick.RemoveListener(OnReadyClicked);
    }

    private void OnReadyClicked()
    {
        if (!IsClient) return;

        isLocalReady = !isLocalReady; // toggle ready status
        UpdateButtonText();

        if (isLocalReady)
        {
            SetReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            UnsetReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        // Solo player check: meteen starten als alleen
        if (NetworkManager.Singleton.ConnectedClients.Count == 1 && isLocalReady)
        {
            if (IsServer)
                SwitchSceneClientRpc();
            else
                RequestSceneStartServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void UpdateButtonText()
    {
        if (readyButton == null) return;

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

    [ServerRpc(RequireOwnership = false)]
    private void RequestSceneStartServerRpc(ulong clientId)
    {
        if (IsServer)
            SwitchSceneClientRpc();
    }

    private void CheckAllReady()
    {
        int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;

        if (readyClients.Count == totalPlayers && totalPlayers > 1)
        {
            // Iedereen ready → switch scene
            SwitchSceneClientRpc();
        }
    }

    [ClientRpc]
    private void UpdateReadyStatusClientRpc(ulong[] readyClientIds)
    {
        readyClients = new List<ulong>(readyClientIds);
        UpdateReadyStatusUI();
    }

    [ClientRpc]
    private void SwitchSceneClientRpc()
    {
        SceneManager.LoadSceneAsync(1);
    }

    private void UpdateReadyStatusUI()
    {
        if (readyStatusText == null) return;

        string status = "Ready Players:\n";
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            bool isReady = readyClients.Contains(client.ClientId);
            status += $"Player {client.ClientId}: {(isReady ? "Ready" : "Not Ready")}\n";
        }
        readyStatusText.text = status;
    }
}
