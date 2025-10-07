using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Linq;
using System.Collections.Generic;

public class CoinPickup : NetworkBehaviour
{
    [Header("Cash Amount")]
    public int cashAmount = 100;

    [Header("Interact afstand")]
    public float interactDistance = 3f;

    private bool pickedUp = false;
    private bool isInteractVisible = false;

    private TextMeshProUGUI localPlayerInteractText;

    private static List<CoinPickup> activeCoins = new List<CoinPickup>();

    private void OnEnable() => activeCoins.Add(this);
    private void OnDisable() => activeCoins.Remove(this);

    private void Update()
    {
        if (!IsClient) return;
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer == null) return;

        if (localPlayerInteractText == null)
        {
            localPlayerInteractText = localPlayer
                .GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(t => t.name == "CashE");
            if (localPlayerInteractText != null)
                localPlayerInteractText.gameObject.SetActive(false);
        }

        if (localPlayerInteractText == null) return;

        bool coinNearby = IsCoinNearby(localPlayer.transform);
        SetInteractTextVisible(coinNearby);

        if (coinNearby && Input.GetKeyDown(KeyCode.E))
        {
            CoinPickup nearestCoin = activeCoins
                .Where(c => !c.pickedUp)
                .OrderBy(c => Vector3.Distance(localPlayer.transform.position, c.transform.position))
                .FirstOrDefault(c => Vector3.Distance(localPlayer.transform.position, c.transform.position) <= interactDistance);

            if (nearestCoin != null)
            {
                PlayerCash playerCash = localPlayer.GetComponent<PlayerCash>();
                nearestCoin.PickupCoinServerRpc(playerCash.NetworkObjectId);
            }
        }
    }

    private void SetInteractTextVisible(bool visible)
    {
        if (localPlayerInteractText == null) return;
        if (isInteractVisible == visible) return;

        localPlayerInteractText.gameObject.SetActive(visible);
        isInteractVisible = visible;
    }

    [ServerRpc(RequireOwnership = false)]
    private void PickupCoinServerRpc(ulong playerId)
    {
        if (pickedUp) return;

        pickedUp = true;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerNetObj))
        {
            PlayerCash playerCash = playerNetObj.GetComponent<PlayerCash>();
            if (playerCash != null)
            {
                // Voegt cash toe via ServerRpc
                playerCash.AddCashServerRpc(cashAmount);
            }
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Despawn();
        else
            Destroy(gameObject);
    }

    private bool IsCoinNearby(Transform playerTransform)
    {
        return activeCoins.Any(c => !c.pickedUp && Vector3.Distance(playerTransform.position, c.transform.position) <= interactDistance);
    }
}
