using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class CoinPickup : NetworkBehaviour
{
    [Header("Cash amount")]
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

        var nm = NetworkManager.Singleton;
        if (!nm.ConnectedClients.TryGetValue(nm.LocalClientId, out var client)) return;
        if (client?.PlayerObject == null) return;

        var localPlayer = client.PlayerObject;

        if (localPlayerInteractText == null)
        {
            localPlayerInteractText = localPlayer
                .GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(t => t.name == "CashE");

            if (localPlayerInteractText != null)
                localPlayerInteractText.gameObject.SetActive(false);
        }

        if (localPlayerInteractText == null) return;

        bool coinNearby = activeCoins.Any(c => !c.pickedUp &&
                                               Vector3.Distance(c.transform.position, localPlayer.transform.position) <= interactDistance);
        SetInteractTextVisible(coinNearby);

        if (coinNearby && Input.GetKeyDown(KeyCode.E))
        {
            CoinPickup nearestCoin = activeCoins
                .Where(c => !c.pickedUp)
                .OrderBy(c => Vector3.Distance(localPlayer.transform.position, c.transform.position))
                .FirstOrDefault(c => Vector3.Distance(localPlayer.transform.position, c.transform.position) <= interactDistance);

            if (nearestCoin != null)
            {
                nearestCoin.TryPickupClientSide(nm.LocalClientId);
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

    private void TryPickupClientSide(ulong clientId)
    {
        if (pickedUp) return;

        pickedUp = true;

        // 🔹 Directe clientside feedback
        var nm = NetworkManager.Singleton;
        if (nm.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var player = client.PlayerObject;
            if (player != null)
            {
                var playerCash = player.GetComponent<PlayerCash>();
                if (playerCash != null)
                {
                    playerCash.AddCashLocal(cashAmount); // lokale cash wijziging
                }
            }
        }

        gameObject.SetActive(false); // verdwijnt meteen

        // 🔹 Vraag server om bevestiging
        PickupCoinServerRpc(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PickupCoinServerRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null)
        {
            PlayerCash playerCash = client.PlayerObject.GetComponent<PlayerCash>();
            if (playerCash != null)
                playerCash.AddCashServerRpc(cashAmount);
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
    }
}
