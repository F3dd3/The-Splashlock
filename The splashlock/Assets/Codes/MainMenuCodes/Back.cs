using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro; // Voor TextMeshPro

public class Back : NetworkBehaviour
{
    [Header("UI Elements")]
    public Button readyButton;
    public TMP_Text readyStatusText; // TextMeshPro

    // Server-side lijst van ready clients
    private List<ulong> readyClients = new List<ulong>();

    private void Start()
    {
        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyClicked);

        UpdateReadyStatusUI();
    }

    private void OnDestroy()
    {
        if (readyButton != null)
            readyButton.onClick.RemoveListener(OnReadyClicked);
    }

    private void OnReadyClicked()
    {
        if (!IsClient) return;

        // Vraag server om jou als ready te markeren
        SetReadyServerRpc(NetworkManager.Singleton.LocalClientId);

        // Disable de knop zodat je niet meerdere keren klikt
        readyButton.interactable = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyServerRpc(ulong clientId)
    {
        if (!readyClients.Contains(clientId))
            readyClients.Add(clientId);

        // Update alle clients met de huidige ready status
        UpdateReadyStatusClientRpc(readyClients.ToArray());

        // Check of iedereen ready is
        CheckAllReady();
    }

    private void CheckAllReady()
    {
        int totalPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;

        if (readyClients.Count == totalPlayers)
        {
            // Iedereen ready → switch scene voor iedereen
            SwitchSceneClientRpc();
        }
    }

    [ClientRpc]
    private void UpdateReadyStatusClientRpc(ulong[] readyClientIds)
    {
        // Update lokale lijst
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
