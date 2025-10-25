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
                cm.shiftLockImage.enabled = false;
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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnLeaveClicked()
    {
        ResumeGame();

        // ✅ Toon loading screen
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingScreenClientRpc("Lobby");
        }

        // Laad de lobby scene met delay
        StartCoroutine(LoadLobbyScene());
    }

    private System.Collections.IEnumerator LoadLobbyScene()
    {
        // Kleine delay zodat loading screen goed zichtbaar wordt (optioneel)
        yield return new WaitForSeconds(0.1f);

        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
                NetworkManager.Singleton.Shutdown();
        }

        // Lobby laden
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainLobby");
        asyncLoad.allowSceneActivation = true;

        // Wacht tot de scene geladen is
        while (!asyncLoad.isDone)
            yield return null;

        // ✅ Verberg loading screen na delay
        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.HideLoadingScreenClientRpc();
    }
}
