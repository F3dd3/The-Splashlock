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
        UpdateReadyStatusClientRpc();
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
        readyButton.GetComponentInChildren<TextMeshProUGUI>().text = isLocalReady ? "Cancel Ready" : "Ready";
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyServerRpc(ulong clientId)
    {
        if (!readyClients.Contains(clientId))
            readyClients.Add(clientId);

        UpdateReadyStatusClientRpc();
        CheckAllReady();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnsetReadyServerRpc(ulong clientId)
    {
        if (readyClients.Contains(clientId))
            readyClients.Remove(clientId);

        UpdateReadyStatusClientRpc();
    }

    [ClientRpc]
    private void UpdateReadyStatusClientRpc()
    {
        if (NetworkManager.Singleton.ConnectedClients.Count <= 1)
        {
            readyStatusText.text = "";
            return;
        }

        string status = "Ready Players:\n";
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            bool isReady = readyClients.Contains(client.ClientId);
            status += $"Player {client.ClientId}: {(isReady ? "Ready" : "Not Ready")}\n";
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

        UpdateReadyStatusClientRpc();
    }
}
