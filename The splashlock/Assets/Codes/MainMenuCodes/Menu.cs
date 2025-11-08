using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Menu : MonoBehaviour
{
    public Button backToLobbyButton;

    private void Start()
    {
        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
    }

    private async void OnBackToLobbyClicked()
    {
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                // Host verlaat de huidige scene en laadt MainLobby
                await lobbyManager.HandleClientOrHostLeftAsync();
            }
            else
            {
                Debug.Log("[Menu] Client volgt scene switch van host automatisch.");
            }
        }

        // ✅ Zorg dat de cursor zichtbaar blijft en niet locked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
