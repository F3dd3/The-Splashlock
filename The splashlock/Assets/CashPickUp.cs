using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Linq;

public class CoinPickup : NetworkBehaviour
{
    [Header("Cash Amount")]
    public int cashAmount = 100;

    [Header("Interact afstand")]
    public float interactDistance = 3f;

    private bool pickedUp = false;
    private bool isInteractVisible = false;

    private TextMeshProUGUI localPlayerInteractText;

    private void Update()
    {
        if (!IsOwner || pickedUp) return;

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer == null) return;

        // Vind CashE TMP op de local player (child van Canvas)
        if (localPlayerInteractText == null)
        {
            localPlayerInteractText = localPlayer
                .GetComponentsInChildren<TextMeshProUGUI>(true) // <- include inactive
                .FirstOrDefault(t => t.name == "CashE");

            if (localPlayerInteractText != null)
                localPlayerInteractText.gameObject.SetActive(false);
        }

        if (localPlayerInteractText == null) return;

        float distance = Vector3.Distance(localPlayer.transform.position, transform.position);

        if (distance <= interactDistance)
        {
            SetInteractTextVisible(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                PlayerCash playerCash = localPlayer.GetComponent<PlayerCash>();
                TryPickup(playerCash);
            }
        }
        else
        {
            SetInteractTextVisible(false);
        }
    }

    private void SetInteractTextVisible(bool visible)
    {
        if (localPlayerInteractText == null) return;
        if (isInteractVisible == visible) return;

        localPlayerInteractText.gameObject.SetActive(visible);
        isInteractVisible = visible;
    }

    private void TryPickup(PlayerCash playerCash)
    {
        if (playerCash == null || pickedUp) return;

        if (IsServer)
        {
            playerCash.AddCashServerRpc(cashAmount);
            pickedUp = true;
            DespawnCoin();
        }
        else
        {
            SubmitPickupServerRpc(playerCash.NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPickupServerRpc(ulong playerId)
    {
        if (pickedUp) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerNetObj))
        {
            PlayerCash playerCash = playerNetObj.GetComponent<PlayerCash>();
            if (playerCash != null)
            {
                playerCash.AddCashServerRpc(cashAmount);
                pickedUp = true;
                DespawnCoin();
            }
        }
    }

    private void DespawnCoin()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Despawn();
        else
            Destroy(gameObject);
    }
}
