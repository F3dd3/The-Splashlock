using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    private void Start()
    {
        if (IsOwner)
        {
            Debug.Log($"Player {OwnerClientId} ready at position {transform.position}");
        }
    }
}
