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
        // Zoek LobbyManager in de scene
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            // LobbyManager regelt alles: shutdown, lobby load, autohost
            _ = lobbyManager.HandleClientOrHostLeftAsync();
        }
        else
        {
            // fallback
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainLobby");
        }
    }
}
