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

    // Static list om alle actieve coins bij te houden
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
        if (!IsClient) return;
        if (NetworkManager.Singleton.LocalClient.PlayerObject == null) return;

        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;

        // Vind CashE TMP (include inactive)
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

        if (coinNearby && Input.GetKeyDown(KeyCode.E))
        {
            // Vind de dichtstbijzijnde munt
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
            AddCashServerRpc(playerCash.NetworkObjectId, cashAmount);
        }

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
