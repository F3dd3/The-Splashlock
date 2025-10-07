using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerCash : NetworkBehaviour
{
    private NetworkVariable<int> cash = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int Cash => cash.Value;

    public void SubscribeCash(Action<int> callback)
    {
        cash.OnValueChanged += (oldValue, newValue) => callback?.Invoke(newValue);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddCashServerRpc(int amount)
    {
        cash.Value += amount;
        Debug.Log($"[PlayerCash] +{amount} cash toegevoegd. Huidige cash: {cash.Value}");
    }
}
