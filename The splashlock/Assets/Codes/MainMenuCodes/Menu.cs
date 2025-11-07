using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

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
        // 1. Laad de MainLobby scene
        SceneManager.LoadScene("MainLobby", LoadSceneMode.Single);

        // 2. Wacht tot de MainLobby scene volledig geladen is
        while (SceneManager.GetActiveScene().name != "MainLobby")
            await Task.Yield();

        // 3. Wacht tot LobbyManager geïnstantieerd is
        LobbyManager lobbyManager = null;
        while (lobbyManager == null)
        {
            lobbyManager = FindObjectOfType<LobbyManager>();
            await Task.Yield();
        }

        // 4. Genereer Relay join-code en update infoText
        if (NetworkManager.Singleton.IsHost)
        {
            lobbyManager.GenerateRelayCodeForBackButton();
        }

        // 5. Cursor reset
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
