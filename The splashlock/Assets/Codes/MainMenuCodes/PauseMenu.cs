using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject optionsMenu; // sleep je UI canvas hier in
    public MonoBehaviour playerController; // sleep je player movement script hier in

    private bool isPaused = false;

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
        optionsMenu.SetActive(true); // toon het menu
        if (playerController != null)
            playerController.enabled = false; // zet je movement script uit
        Time.timeScale = 0f; // pauzeert de game (optioneel)
        isPaused = true;
    }

    void ResumeGame()
    {
        optionsMenu.SetActive(false); // verberg menu
        if (playerController != null)
            playerController.enabled = true; // zet je movement script weer aan
        Time.timeScale = 1f; // game weer normaal
        isPaused = false;
    }
}