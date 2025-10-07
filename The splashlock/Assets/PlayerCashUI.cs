using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerCashUI : NetworkBehaviour
{
    [Header("Sleep hier je TextMeshProUGUI")]
    public TextMeshProUGUI cashText;

    private PlayerCash playerCash;

    public override void OnNetworkSpawn()
    {
        playerCash = GetComponent<PlayerCash>();
        if (!IsOwner)
        {
            if (cashText != null)
                cashText.gameObject.SetActive(false);
            return;
        }

        // Luister naar cash updates
        playerCash.OnCashChanged += UpdateCashUI;

        // Initiele waarde tonen
        UpdateCashUI(0, playerCash.Cash);
    }

    private void OnDestroy()
    {
        if (playerCash != null)
            playerCash.OnCashChanged -= UpdateCashUI;
    }

    private void UpdateCashUI(int oldValue, int newValue)
    {
        if (cashText != null)
            cashText.text = $"Cash: ${newValue}";
    }
}
