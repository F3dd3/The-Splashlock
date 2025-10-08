using UnityEngine;
using Unity.Netcode;

public class PlayerCash : NetworkBehaviour
{
    public NetworkVariable<int> Cash = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [ServerRpc(RequireOwnership = false)]
    public void AddCashServerRpc(int amount)
    {
        Cash.Value += amount;
        Debug.Log($"[Server] +{amount} cash toegevoegd aan speler {OwnerClientId}. Totaal: {Cash.Value}");
    }

    // Wordt alleen lokaal op client aangeroepen (voor visuele feedback)
    public void AddCashLocal(int amount)
    {
        if (IsOwner)
        {
            Cash.Value += amount; // lokale schatting
            Debug.Log($"[Client] +{amount} cash lokaal toegevoegd (prediction).");
        }
    }
}
