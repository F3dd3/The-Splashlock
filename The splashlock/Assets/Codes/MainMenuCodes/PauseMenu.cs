using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class PauseMenu : NetworkBehaviour
{
    [Header("UI")]
    public GameObject optionsMenu;
    public Button leaveButton;

    [Header("Player Control")]
    public MonoBehaviour playerController;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Start()
    {
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeaveClicked);

        if (optionsMenu != null)
            optionsMenu.SetActive(false);
    }

    private void OnDestroy()
    {
        if (!IsOwner) return;
        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(OnLeaveClicked);
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (optionsMenu != null)
            optionsMenu.SetActive(true);

        if (playerController != null)
            playerController.enabled = false;

        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (optionsMenu != null)
            optionsMenu.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;

        isPaused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private async void OnLeaveClicked()
    {
        ResumeGame();

        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("[PauseMenu] Host leave: stop host, clients worden disconnect");

                NetworkManager.Singleton.Shutdown();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                Debug.Log("[PauseMenu] Client leave: disconnect van host");
                NetworkManager.Singleton.Shutdown();
            }
        }

        await Task.Delay(500);

        // Load eigen lobby
        if (SceneManager.GetActiveScene().name != "MainLobby")
            SceneManager.LoadScene("MainLobby");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            Debug.Log("[PauseMenu] Auto-host starten in eigen lobby");
            lobbyManager.AutoHostGame();
        }
    }
}
