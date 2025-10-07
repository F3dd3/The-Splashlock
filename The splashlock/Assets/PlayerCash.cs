using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerCash : NetworkBehaviour
{
    private NetworkVariable<int> cash = new NetworkVariable<int>(0);
    public int Cash => cash.Value;

    public event Action<int, int> OnCashChanged;

    public override void OnNetworkSpawn()
    {
        // Luister naar NetworkVariable veranderingen
        cash.OnValueChanged += (oldValue, newValue) => OnCashChanged?.Invoke(oldValue, newValue);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddCashServerRpc(int amount)
    {
        cash.Value += amount;
    }
}
