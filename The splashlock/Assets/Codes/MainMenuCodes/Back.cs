using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class Back : NetworkBehaviour
{
    public Button readyButton;
    public TextMeshProUGUI readyStatusText;

    private List<ulong> readyClients = new List<ulong>();
    private bool isLocalReady = false;

    private void Start()
    {
        readyButton.onClick.AddListener(OnReadyClicked);
        UpdateButtonText();
        UpdateReadyStatusUI();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoined;
        }
    }

    private void OnDestroy()
    {
        readyButton.onClick.RemoveListener(OnReadyClicked);
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientJoined;
        }
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

    private void CheckAllReady()
    {
        int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;
        if (readyClients.Count == totalPlayers && totalPlayers > 0)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    [ClientRpc]
    private void UpdateReadyStatusClientRpc(ulong[] readyClientIds, ClientRpcParams rpcParams = default)
    {
        readyClients = new List<ulong>(readyClientIds);
        UpdateReadyStatusUI();
    }

    private void UpdateReadyStatusUI()
    {
        // Alleen tonen als er meerdere spelers zijn
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

    // Callback bij join van nieuwe client
    private void OnClientJoined(ulong clientId)
    {
        if (!IsServer) return;

        // ✅ Update de host zelf direct
        UpdateReadyStatusClientRpc(readyClients.ToArray());

        // ✅ Stuur huidige ready-status naar de nieuw joinende client
        UpdateReadyStatusClientRpc(readyClients.ToArray(), new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });
    }
}
