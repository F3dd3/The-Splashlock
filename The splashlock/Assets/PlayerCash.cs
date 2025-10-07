using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerCash : NetworkBehaviour
{
    // Netwerkvariabele die uniek is per speler
    private NetworkVariable<int> cash = new NetworkVariable<int>(
        0, // startwaarde
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Publiek event voor andere scripts
    public event Action<int, int> OnCashChanged;

    public int Cash => cash.Value;

    private void Start()
    {
        // Luister naar veranderingen
        cash.OnValueChanged += HandleCashChanged;
    }

    private void OnDestroy()
    {
        cash.OnValueChanged -= HandleCashChanged;
    }

    private void HandleCashChanged(int oldValue, int newValue)
    {
        OnCashChanged?.Invoke(oldValue, newValue);
        if (IsOwner)
        {
            Debug.Log($"[Client] Jouw geld is gewijzigd: {oldValue} → {newValue}");
        }
    }

    // Alleen de server mag geld aanpassen
    [ServerRpc(RequireOwnership = false)]
    public void AddCashServerRpc(int amount)
    {
        cash.Value += amount;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCashServerRpc(int newAmount)
    {
        cash.Value = newAmount;
    }
}
