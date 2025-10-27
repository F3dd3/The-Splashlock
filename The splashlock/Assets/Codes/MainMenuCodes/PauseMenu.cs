using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class PauseMenu : NetworkBehaviour
{
    [Header("UI")]
    public GameObject optionsMenu;
    public Button leaveButton;

    [Header("Player Control")]
    public MonoBehaviour playerController;

    private bool isPaused = false;
    public bool IsPaused => isPaused; // property voor andere scripts

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
            optionsMenu.SetActive(false); // canvas standaard uit
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

    void PauseGame()
    {
        if (optionsMenu != null)
            optionsMenu.SetActive(true);

        if (playerController != null)
            playerController.enabled = false;

        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ResumeGame()
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

        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.ShowLoadingScreenClientRpc("Lobby");

        StartCoroutine(LoadLobbyScene());
    }

    private IEnumerator LoadLobbyScene()
    {
        yield return new WaitForSeconds(0.1f);

        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
                NetworkManager.Singleton.Shutdown();
        }

        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainLobby");
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
            yield return null;

        if (LoadingScreenManager.Instance != null)
            LoadingScreenManager.Instance.HideLoadingScreenClientRpc();
    }
}
