using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WinScreenTrigger : MonoBehaviour
{
    [Header("Win Screen Canvas")]
    public GameObject winScreenCanvas; // Het Canvas met jouw Win Screen UI

    [Header("Speler")]
    public CharacterMovement playerMovement; // De speler met CharacterMovement script

    [Header("Lobby Scene Name")]
    public string lobbySceneName = "Lobby"; // Naam van je lobby scene

    private void Start()
    {
        // Canvas uit bij start
        if (winScreenCanvas != null)
            winScreenCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check of speler een CharacterController heeft
        if (other.GetComponent<CharacterController>() != null)
        {
            // Win screen tonen
            if (winScreenCanvas != null)
                winScreenCanvas.SetActive(true);

            // Spelerbeweging stoppen
            if (playerMovement != null)
                playerMovement.enabled = false;

            // Cursor zichtbaar maken voor UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Functie voor de Quit-knop
    public void QuitToLobby()
    {
        SceneManager.LoadScene(lobbySceneName);
    }
}
