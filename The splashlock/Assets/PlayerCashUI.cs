using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerCashUI : MonoBehaviour
{
    public TextMeshProUGUI cashText;
    private PlayerCash playerCash;

    private void Start()
    {
        InvokeRepeating(nameof(FindLocalPlayerCash), 0.5f, 0.5f);
    }

    private void FindLocalPlayerCash()
    {
        if (playerCash != null) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        var nm = NetworkManager.Singleton;
        if (!nm.ConnectedClients.TryGetValue(nm.LocalClientId, out var client)) return;
        if (client?.PlayerObject == null) return;

        playerCash = client.PlayerObject.GetComponent<PlayerCash>();
        if (playerCash == null) return;

        playerCash.Cash.OnValueChanged += (oldValue, newValue) => UpdateCashUI(newValue);
        UpdateCashUI(playerCash.Cash.Value);

        CancelInvoke(nameof(FindLocalPlayerCash));
    }

    private void UpdateCashUI(int newValue)
    {
        if (cashText != null)
            cashText.text = $"Cash: ${newValue}";
    }
}
