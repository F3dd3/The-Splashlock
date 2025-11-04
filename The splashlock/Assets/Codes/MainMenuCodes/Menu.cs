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
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            // Host triggers the scene switch; clients follow automatically
            _ = lobbyManager.HandleClientOrHostLeftAsync();
        }
    }
}
