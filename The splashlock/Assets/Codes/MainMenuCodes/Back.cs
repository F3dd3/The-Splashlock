using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class Back : NetworkBehaviour
{
    public static Back Instance;

    public Button readyButton;

    [Header("Maps Settings")]
    public List<string> selectableMaps = new List<string>();

    private List<ulong> readyClients = new List<ulong>();
    private bool isLocalReady = false;

    private Dictionary<string, int> consecutivePlays = new Dictionary<string, int>();

    private void Awake()
    {
        Instance = this;
        foreach (string map in selectableMaps)
            consecutivePlays[map] = 0;
    }

    private void Start()
    {
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyClicked);
            UpdateButtonText();
            readyButton.gameObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (readyButton != null)
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

        if (readyClients.Count == NetworkManager.Singleton.ConnectedClients.Count)
        {
            StartGame();
        }
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

        foreach (var player in FindObjectsOfType<Player>())
        {
            if (player != null)
            {
                bool isReady = readyClients.Contains(player.OwnerClientId);
                player.SetReadyText(isReady);
            }
        }
    }

    public void RemoveClientReadyStatus(ulong clientId)
    {
        if (readyClients.Contains(clientId))
            readyClients.Remove(clientId);

        UpdateReadyStatusClientRpc(readyClients.ToArray());
    }

    public void ResetReadyStatus()
    {
        readyClients.Clear();
        isLocalReady = false;
        UpdateButtonText();
    }

    private void StartGame()
    {
        if (selectableMaps.Count == 0)
        {
            Debug.LogWarning("Geen maps beschikbaar om te laden!");
            return;
        }

        string selectedMap = selectableMaps[Random.Range(0, selectableMaps.Count)];
        Debug.Log("Geselecteerde map: " + selectedMap);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                selectedMap,
                UnityEngine.SceneManagement.LoadSceneMode.Single
            );
        }
    }

    // ✅ Full reset bij exit
    public void FullReset()
    {
        readyClients.Clear();
        isLocalReady = false;
        UpdateButtonText();
        consecutivePlays.Clear();
    }
}
