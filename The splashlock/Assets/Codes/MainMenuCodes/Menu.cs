using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class Menu : NetworkBehaviour
{
    public Button backToLobbyButton;

    private void Start()
    {
        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
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
