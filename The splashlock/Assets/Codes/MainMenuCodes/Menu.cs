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
        WinScreenTrigger winScreen = FindObjectOfType<WinScreenTrigger>();
        if (winScreen != null)
        {
            winScreen.ReturnToLobby();
        }

        // Cursor en playerController worden hier niet aangepast
    }
}
