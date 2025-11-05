using UnityEngine;
using Unity.Netcode;

public class WinScreenTrigger : NetworkBehaviour
{
    [Header("Speler")]
    public CharacterMovement playerMovement;

    private bool hasTriggered = false; // voorkomt dubbele triggers

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        CharacterController controller = other.GetComponent<CharacterController>();
        if (controller == null) return;

        NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
        if (playerNetObj == null) return;

        ulong winningClientId = playerNetObj.OwnerClientId;

        // Roep ServerRpc aan zodat de server iedereen updatet
        TriggerWinServerRpc(winningClientId);

        // Lokale speler beweging uitschakelen
        if (playerMovement != null)
            playerMovement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        hasTriggered = true; // zorg dat deze trigger niet opnieuw wordt geactiveerd
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerWinServerRpc(ulong winningClientId, ServerRpcParams rpcParams = default)
    {
        WinScreenManager winManager = FindObjectOfType<WinScreenManager>();
        if (winManager != null)
        {
            winManager.ShowWinScreenClientRpc(winningClientId);
        }
    }

    public void ReturnToLobby()
    {
        Back.Instance?.ResetReadyStatus();

        if (playerMovement != null)
            playerMovement.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
