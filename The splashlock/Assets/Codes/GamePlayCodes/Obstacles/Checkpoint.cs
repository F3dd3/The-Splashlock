using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Update()
    {
        if (CheckpointManager.Instance == null) return;

        // Vind speler in de scene (of via tag)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Check of speler in trigger zit
        if (col.bounds.Contains(player.transform.position))
        {
            CheckpointManager.Instance.ActivateCheckpoint(gameObject, player);
        }
    }
}
