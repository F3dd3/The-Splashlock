using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject optionsMenu; // Sleep hier je pauze-menu canvas in
    public Button leaveButton;     // Sleep hier de "Leave to Lobby" knop in

    [Header("Player Control")]
    public MonoBehaviour playerController; // Sleep je player movement script hier in

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
            if (isPaused)
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

        Time.timeScale = 0f;
        isPaused = true;
    }

    void ResumeGame()
    {
        optionsMenu.SetActive(false);
        if (playerController != null)
            playerController.enabled = true;

        Time.timeScale = 1f;
        isPaused = false;
    }

    /// <summary>
    /// Wordt aangeroepen wanneer speler op "Leave to Lobby" drukt.
    /// </summary>
    private void OnLeaveClicked()
    {
        ResumeGame(); // zorg dat menu en tijd weer normaal zijn

        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("Geen NetworkManager gevonden!");
            SceneManager.LoadScene("MainLobby");
            return;
        }

        if (NetworkManager.Singleton.IsHost)
        {
            // Host sluit alleen zijn eigen sessie, anderen blijven
            Debug.Log("Host verlaat game en gaat terug naar lobby.");
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainLobby");
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            // Client disconnect zichzelf van de server
            Debug.Log("Client verlaat game en gaat terug naar lobby.");
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainLobby");
        }
        else
        {
            // Als er geen verbinding actief is
            SceneManager.LoadScene("MainLobby");
        }
    }
}
