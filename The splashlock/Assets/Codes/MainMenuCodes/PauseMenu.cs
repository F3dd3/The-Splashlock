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

        // Forceer shift lock uit en verberg ShiftLock UI
        CharacterMovement cm = playerController as CharacterMovement;
        if (cm != null)
        {
            cm.shiftLockEnabled = false;
            if (cm.shiftLockImage != null)
                cm.shiftLockImage.enabled = false; // verdwijnt zodra menu opent
        }

        // Cursor zichtbaar in menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ResumeGame()
    {
        optionsMenu.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;

        IsPaused = false;

        // Cursor zichtbaar houden totdat shift lock wordt ingeschakeld
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
