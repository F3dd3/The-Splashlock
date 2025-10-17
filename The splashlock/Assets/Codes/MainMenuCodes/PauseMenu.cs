using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject optionsMenu;
    public Button leaveButton;

    [Header("Player Control")]
    public MonoBehaviour playerController;

    private bool isPaused = false;

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
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    void PauseGame()
    {
        optionsMenu.SetActive(true);
        if (playerController != null) playerController.enabled = false;
        Time.timeScale = 0f;
        isPaused = true;
    }

    void ResumeGame()
    {
        optionsMenu.SetActive(false);
        if (playerController != null) playerController.enabled = true;
        Time.timeScale = 1f;
        isPaused = false;
    }

    private void OnLeaveClicked()
    {
        ResumeGame();

        if (NetworkManager.Singleton == null)
        {
            SceneManager.LoadScene("MainLobby");
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[PauseMenu] Host brengt iedereen terug naar lobby...");
            NetworkManager.Singleton.SceneManager.LoadScene("MainLobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("[PauseMenu] Client verlaat lobby via host scene-change.");
        }
    }
}
