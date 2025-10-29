using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Spawn Point")]
    public Transform spawnPoint;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    /// <summary>
    /// Wordt aangeroepen door speler via PlayerCheckpointDetector
    /// </summary>
    public void OnPlayerTouch(GameObject player)
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning($"Checkpoint '{gameObject.name}' heeft geen spawnPoint ingesteld!");
            return;
        }

        CheckpointManager manager = FindObjectOfType<CheckpointManager>();
        if (manager != null)
            manager.ActivateCheckpoint(this, player);
    }
}
