using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Spawn Point")]
    public Transform spawnPoint;

    [Header("Uniek ID voor Netcode")]
    public int checkpointId; // Gebruik dit i.p.v GetInstanceID

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }
}
