using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerCashUI : NetworkBehaviour
{
    public TextMeshProUGUI cashText;
    private PlayerCash playerCash;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (cashText != null) cashText.gameObject.SetActive(false);
            return;
        }

        playerCash = GetComponent<PlayerCash>();
        if (playerCash == null)
        {
            Debug.LogError("[PlayerCashUI] PlayerCash component niet gevonden!");
            return;
        }

        playerCash.SubscribeCash(UpdateCashUI);

        // Init UI
        UpdateCashUI(playerCash.Cash);
    }

    private void UpdateCashUI(int newValue)
    {
        if (cashText != null)
            cashText.text = $"Cash: ${newValue}";
    }
}
