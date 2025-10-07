using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerCashUI : NetworkBehaviour
{
    [Header("Sleep hier je TextMeshProUGUI voor cash")]
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

        playerCash.OnCashChanged += UpdateCashUI;
        UpdateCashUI(0, playerCash.Cash); // Init
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
