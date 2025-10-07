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

    // Statische lijst van alle actieve munten
    private static List<CoinPickup> activeCoins = new List<CoinPickup>();

    private void OnEnable()
    {
        activeCoins.Add(this);
    }

    private void OnDisable()
    {
        activeCoins.Remove(this);
    }

    private void Update()
    {
        if (!IsClient) return; // alleen clients doen input en UI
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer == null) return;

        // Vind CashE TMP op de local player (include inactive)
        if (localPlayerInteractText == null)
        {
            localPlayerInteractText = localPlayer
                .GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(t => t.name == "CashE");

            if (localPlayerInteractText != null)
                localPlayerInteractText.gameObject.SetActive(false); // standaard uit
        }

        if (localPlayerInteractText == null) return;

        // Check of er een munt dichtbij is
        bool coinNearby = IsCoinNearby(localPlayer.transform);
        SetInteractTextVisible(coinNearby);

        // Als je E drukt, pak de dichtstbijzijnde munt
        if (coinNearby && Input.GetKeyDown(KeyCode.E))
        {
            CoinPickup nearestCoin = activeCoins
                .Where(c => !c.pickedUp)
                .OrderBy(c => Vector3.Distance(localPlayer.transform.position, c.transform.position))
                .FirstOrDefault(c => Vector3.Distance(localPlayer.transform.position, c.transform.position) <= interactDistance);

            if (nearestCoin != null)
            {
                PlayerCash playerCash = localPlayer.GetComponent<PlayerCash>();
                nearestCoin.PickupCoin(playerCash);
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

    public void PickupCoin(PlayerCash playerCash)
    {
        if (pickedUp) return;

        pickedUp = true;

        if (playerCash != null)
        {
            // Vraag server om cash toe te voegen
            AddCashServerRpc(playerCash.NetworkObjectId, cashAmount);
        }

        // Despawn de munt via de server
        DespawnCoin();
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddCashServerRpc(ulong playerId, int amount)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerNetObj))
        {
            PlayerCash playerCash = playerNetObj.GetComponent<PlayerCash>();
            if (playerCash != null)
            {
                playerCash.AddCashServerRpc(amount);
            }
        }
    }

    private void DespawnCoin()
    {
        // Alleen de server kan echt despawnen
        if (IsServer)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.Despawn();
            else
                Destroy(gameObject);
        }
        else
        {
            // Clients vragen de server om te despawnen via ServerRpc
            RequestDespawnServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDespawnServerRpc()
    {
        if (pickedUp)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.Despawn();
            else
                Destroy(gameObject);
        }
    }

    private bool IsCoinNearby(Transform playerTransform)
    {
        return activeCoins.Any(c => !c.pickedUp && Vector3.Distance(playerTransform.position, c.transform.position) <= interactDistance);
    }
}
