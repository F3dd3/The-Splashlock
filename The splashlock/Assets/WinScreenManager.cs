using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class WinScreenManager : NetworkBehaviour
{
    [Header("UI Elements")]
    public GameObject winScreenPanel;
    public Button backToLobbyButton;

    private void Start()
    {
        if (winScreenPanel != null)
            winScreenPanel.SetActive(false);

        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
    }

    /// <summary>
    /// Wordt aangeroepen door de game om de winscreen te tonen
    /// </summary>
    [ClientRpc]
    public void ShowWinScreenClientRpc()
    {
        if (winScreenPanel != null)
            winScreenPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnBackToLobbyClicked()
    {
        if (!IsServer)
        {
            // Alleen host start de overgang
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

        // Laad terug naar de lobby via Netcode
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
