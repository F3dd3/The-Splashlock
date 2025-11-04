using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Threading.Tasks;

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

        // Esc toggle pause
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

    private void OnLeaveClicked()
    {
        ResumeGame();

        // Zoek LobbyManager in de scene
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            // Trigger volledige lobby + autohost flow
            _ = lobbyManager.HandleClientOrHostLeftAsync();
        }
        else
        {
            // fallback: gewoon lobby loaden
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainLobby");
        }
    }
}
