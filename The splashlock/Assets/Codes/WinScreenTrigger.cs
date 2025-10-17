using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class WinScreenTrigger : MonoBehaviour
{
    [Header("Win Screen Canvas")]
    public GameObject winScreenCanvas;

    [Header("Player")]
    public CharacterMovement playerMovement;

    [Header("Back to Lobby Button")]
    public Button backToLobbyButton;

    private void Start()
    {
        if (winScreenCanvas != null)
            winScreenCanvas.SetActive(false);

        if (backToLobbyButton != null)
        {
            backToLobbyButton.gameObject.SetActive(NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost);
            backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
        }
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

    private void OnBackToLobbyClicked()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        // 1️⃣ Host terug naar lobby
        LobbyManager.Instance.ReturnToLobbyFromGame();

        // 2️⃣ Spawn host op zijn plek
        PlayerSpawner.Instance?.SpawnPlayer(NetworkManager.Singleton.LocalClientId, true);

        // 3️⃣ Stuur clients één voor één terug
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == NetworkManager.Singleton.LocalClientId) continue;
            LobbyManager.Instance.SendClientBackToLobbyServerRpc(client.ClientId);
        }

        // 4️⃣ Reset ready stats na delay
        Invoke(nameof(ResetReadyAfterClientsBack), 1f);
    }

    private void ResetReadyAfterClientsBack()
    {
        Back.Instance?.ResetReadyStatus();
    }
}
