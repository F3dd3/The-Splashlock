using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public Button backToLobbyButton;

    private void Start()
    {
        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
    }

    private void OnBackToLobbyClicked()
    {
        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.ShowLoadingScreenClientRpc("MainLobby");

        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            _ = lobbyManager.HandleClientOrHostLeftAsync();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainLobby");
        }
    }
}
