using UnityEngine;
using Unity.Netcode;

public class WinScreenTrigger : NetworkBehaviour
{
    [Header("Speler")]
    public CharacterMovement playerMovement;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        CharacterController controller = other.GetComponent<CharacterController>();
        if (controller == null) return;

        NetworkObject playerNetObj = other.GetComponent<NetworkObject>();
        if (playerNetObj == null) return;

        ulong winningClientId = playerNetObj.OwnerClientId;

        TriggerWinServerRpc(winningClientId);

        if (playerMovement != null)
            playerMovement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        hasTriggered = true;
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
        if (IsOwner)
        {
            ReturnToLobbyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReturnToLobbyServerRpc(ServerRpcParams rpcParams = default)
    {
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null && NetworkManager.Singleton.IsServer)
        {
            _ = lobbyManager.HandleBackToLobbyAsync();
        }
    }
}
