using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerCheckpointDetector : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        Checkpoint cp = other.GetComponent<Checkpoint>();
        if (cp != null)
        {
            Debug.Log($"[Client] checkpoint touched: {cp.name} (ID {cp.checkpointId})");
            ActivateCheckpointServerRpc(cp.checkpointId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateCheckpointServerRpc(int checkpointId, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(senderClientId)) return;

        var client = NetworkManager.Singleton.ConnectedClients[senderClientId];
        if (client == null || client.PlayerObject == null) return;

        GameObject playerGO = client.PlayerObject.gameObject;

        // Vind checkpoint via ID
        Checkpoint cp = null;
        Checkpoint[] all = FindObjectsOfType<Checkpoint>();
        foreach (var c in all)
        {
            if (c.checkpointId == checkpointId)
            {
                cp = c;
                break;
            }
        }

        if (cp == null) return;

        CheckpointManager manager = FindObjectOfType<CheckpointManager>();
        if (manager != null)
            manager.ActivateCheckpoint(cp, playerGO);
    }
}
