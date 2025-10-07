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

    // Lijst van actieve munten
    private static List<CoinPickup> activeCoins = new List<CoinPickup>();

    private void OnEnable() => activeCoins.Add(this);
    private void OnDisable() => activeCoins.Remove(this);

    private void Update()
    {
        if (!IsClient) return;

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer == null) return;

        // Vind CashE TMP
        if (localPlayerInteractText == null)
        {
            localPlayerInteractText = localPlayer
                .GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(t => t.name == "CashE");

            if (localPlayerInteractText != null)
                localPlayerInteractText.gameObject.SetActive(false);
        }

        if (localPlayerInteractText == null) return;

        // Check munten dichtbij
        bool coinNearby = IsCoinNearby(localPlayer.transform);
        SetInteractTextVisible(coinNearby);

        // Interactie
        if (coinNearby && Input.GetKeyDown(KeyCode.E))
        {
            CoinPickup nearestCoin = activeCoins
                .Where(c => !c.pickedUp)
                .OrderBy(c => Vector3.Distance(localPlayer.transform.position, c.transform.position))
                .FirstOrDefault(c => Vector3.Distance(localPlayer.transform.position, c.transform.position) <= interactDistance);

            if (nearestCoin != null)
            {
                PlayerCash playerCash = localPlayer.GetComponent<PlayerCash>();

                // Vraag de server om munt op te pakken en cash toe te voegen
                nearestCoin.PickupCoinServerRpc(playerCash.NetworkObjectId);

                // E direct verbergen voor lokale feedback
                SetInteractTextVisible(false);
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

        // Voeg cash toe aan speler via NetworkVariable
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerNetObj))
        {
            PlayerCash playerCash = playerNetObj.GetComponent<PlayerCash>();
            if (playerCash != null)
                playerCash.AddCashServerRpc(cashAmount); // Dit triggert OnValueChanged op eigenaar
        }

        // Despawn coin voor alle clients
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
