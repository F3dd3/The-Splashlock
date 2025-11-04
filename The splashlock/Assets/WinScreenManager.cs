using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class WinScreenManager : NetworkBehaviour
{
    [Header("UI Elements")]
    public GameObject winScreenPanel;
    public Button backToLobbyButton;

    [Header("Win Screens per Client")]
    public GameObject[] winScreens; // Sleep hier je 4 win screen images in

    // Mapping van clientId naar winscreen index
    private Dictionary<ulong, int> clientIdToWinScreenIndex = new Dictionary<ulong, int>();

    private void Awake()
    {
        if (winScreenPanel != null)
            winScreenPanel.SetActive(false);

        // Zet alle individuele winScreens uit
        if (winScreens != null)
        {
            foreach (var ws in winScreens)
                ws.SetActive(false);
        }

        // Maak vaste mapping van clientId naar winscreen index
        if (NetworkManager.Singleton != null)
        {
            var sortedClients = NetworkManager.Singleton.ConnectedClientsList
                                .OrderBy(c => c.ClientId)
                                .ToList();

            for (int i = 0; i < sortedClients.Count && i < winScreens.Length; i++)
            {
                clientIdToWinScreenIndex[sortedClients[i].ClientId] = i;
            }
        }

        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
    }

    /// <summary>
    /// Wordt door de server aangeroepen om de juiste winscreen te tonen
    /// </summary>
    [ClientRpc]
    public void ShowWinScreenClientRpc(ulong winningClientId)
    {
        if (winScreenPanel != null)
            winScreenPanel.SetActive(true);

        if (winScreens != null)
        {
            foreach (var ws in winScreens)
                ws.SetActive(false);

            if (clientIdToWinScreenIndex.TryGetValue(winningClientId, out int index))
            {
                if (index >= 0 && index < winScreens.Length)
                    winScreens[index].SetActive(true);
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnBackToLobbyClicked()
    {
        if (!IsServer)
        {
            RequestBackToLobbyServerRpc();
        }
        else
        {
            StartCoroutine(LoadLobbySceneRoutine());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBackToLobbyServerRpc(ServerRpcParams rpcParams = default)
    {
        StartCoroutine(LoadLobbySceneRoutine());
    }

    private IEnumerator LoadLobbySceneRoutine()
    {
        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.ShowLoadingScreenClientRpc("MainLobby");

        yield return new WaitForSeconds(0.3f);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                "MainLobby",
                UnityEngine.SceneManagement.LoadSceneMode.Single
            );
        }

        yield return null;
    }
}
