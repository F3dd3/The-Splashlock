using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject optionsMenu;
    public Button leaveButton;

    [Header("Player Control")]
    public MonoBehaviour playerController;

    public static bool IsPaused = false;
    public static bool LeavingToLobby = false;

    private void Start()
    {
        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeaveClicked);
    }

    private void OnDestroy()
    {
        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(OnLeaveClicked);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    void PauseGame()
    {
        optionsMenu.SetActive(true);

        if (playerController != null)
            playerController.enabled = false;

        IsPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ResumeGame()
    {
        optionsMenu.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;

        IsPaused = false;

        CharacterMovement cm = playerController as CharacterMovement;

        // Check: in-game en shiftlock aan → cursor direct verbergen
        if (!LeavingToLobby && cm != null && cm.shiftLockEnabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnLeaveClicked()
    {
        LeavingToLobby = true;
        ResumeGame();

        if (NetworkManager.Singleton == null)
        {
            SceneManager.LoadScene("MainLobby");
            return;
        }

        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainLobby");
        }
        else
        {
            SceneManager.LoadScene("MainLobby");
        }
    }
}
