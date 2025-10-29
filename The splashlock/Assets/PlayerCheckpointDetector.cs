using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerCheckpointDetector : NetworkBehaviour
{
    public float detectRadius = 0.5f;
    public LayerMask checkpointLayer;

    private void Update()
    {
        if (!IsOwner) return; // Alleen de eigenaar detecteert zijn eigen checkpoints

        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, checkpointLayer);
        if (hits.Length > 0)
        {
            Debug.Log($"PlayerCheckpointDetector: OverlapSphere vond {hits.Length} colliders");
        }
        foreach (Collider hit in hits)
        {
            Checkpoint cp = hit.GetComponent<Checkpoint>();
            if (cp != null)
            {
                Debug.Log($"PlayerCheckpointDetector: checkpoint detectie local: {cp.name}");
                // Stuur naar server het InstanceID van het checkpoint zodat de server het juiste object kan vinden
                ActivateCheckpointServerRpc(cp.gameObject.GetInstanceID());
            }
        }
    }

    // ServerRpc om de server te laten registreren welk checkpoint deze speler activeerde
    [ServerRpc(RequireOwnership = false)]
    private void ActivateCheckpointServerRpc(int checkpointInstanceId, ServerRpcParams rpcParams = default)
    {
        // Vind de checkpoint in de scène via InstanceID
        Checkpoint found = null;
        Checkpoint[] all = FindObjectsOfType<Checkpoint>();
        foreach (var c in all)
        {
            if (c.gameObject.GetInstanceID() == checkpointInstanceId)
            {
                found = c;
                break;
            }
        }

        if (found == null)
        {
            Debug.LogWarning($"ActivateCheckpointServerRpc: kon checkpoint met InstanceID {checkpointInstanceId} niet vinden.");
            return;
        }

        // Vind het player GameObject van de caller
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(senderClientId))
        {
            Debug.LogWarning($"ActivateCheckpointServerRpc: geen connected client voor id {senderClientId}");
            return;
        }

        var client = NetworkManager.Singleton.ConnectedClients[senderClientId];
        if (client == null || client.PlayerObject == null)
        {
            Debug.LogWarning($"ActivateCheckpointServerRpc: geen playerobject voor client {senderClientId}");
            return;
        }

        GameObject playerGO = client.PlayerObject.gameObject;

        // Laat de manager (server-side) het checkpoint registreren
        CheckpointManager manager = FindObjectOfType<CheckpointManager>();
        if (manager == null)
        {
            Debug.LogWarning("ActivateCheckpointServerRpc: geen CheckpointManager in scene gevonden");
            return;
        }

        manager.ActivateCheckpoint(found, playerGO);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
