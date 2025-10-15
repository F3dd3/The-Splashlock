using UnityEngine;
using Unity.Netcode;

public class WinScreenTrigger : MonoBehaviour
{
    [Header("Win Screen Canvas")]
    public GameObject winScreenCanvas;

    [Header("Speler")]
    public CharacterMovement playerMovement;

    private void Start()
    {
        if (winScreenCanvas != null)
            winScreenCanvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>() != null)
        {
            if (winScreenCanvas != null)
                winScreenCanvas.SetActive(true);

            if (playerMovement != null)
                playerMovement.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ReturnToLobby()
    {
        if (winScreenCanvas != null)
            winScreenCanvas.SetActive(false);

        // Reset spelers naar lobby spawnpunten en kleuren
        PlayerSpawner.Instance?.ResetForLobby();

        // Reset ready status
        Back.Instance?.ResetReadyStatus();

        // Lokale speler beweging weer inschakelen
        if (playerMovement != null)
            playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
