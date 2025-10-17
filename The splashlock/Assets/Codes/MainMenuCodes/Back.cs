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

    [Header("Maps Settings")]
    [Tooltip("Kies hier welke maps mogelijk zijn.")]
    public List<string> selectableMaps = new List<string>();

    private List<ulong> readyClients = new List<ulong>();
    private bool isLocalReady = false;

    private string lastPlayedMap = ""; // Houd bij welke map laatst gespeeld is

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        readyButton.onClick.AddListener(OnReadyClicked);
        UpdateButtonText();

        readyButton.gameObject.SetActive(true);
        SetReadyStatsVisible(false);

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
            status += $"Player {client.ClientId}: {(isReady ? "Ready" : "Not Ready")}\n";
        }

        readyStatusText.text = status;
    }

    private void CheckAllReady()
    {
        int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;
        if (readyClients.Count == totalPlayers && totalPlayers > 0)
        {
            string chosenMap = ChooseMapWithDynamicChance();
            lastPlayedMap = chosenMap;

            Debug.Log($"[Server] Alle spelers ready. Laden map: {chosenMap}");

            NetworkManager.Singleton.SceneManager.LoadScene(chosenMap, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private string ChooseMapWithDynamicChance()
    {
        if (selectableMaps.Count == 0) return "GameScene";

        // Maak een lijst van gewichten
        List<float> weights = new List<float>();
        float totalWeight = 0f;

        foreach (string map in selectableMaps)
        {
            float weight = 1f;

            // Als dit de laatst gespeelde map is, halveer de kans
            if (map == lastPlayedMap)
                weight = 0.5f;

            weights.Add(weight);
            totalWeight += weight;
        }

        // Kies een random getal tussen 0 en totalWeight
        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < selectableMaps.Count; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
                return selectableMaps[i];
        }

        // fallback
        return selectableMaps[0];
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
        SetReadyStatsVisible(false);
    }

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
