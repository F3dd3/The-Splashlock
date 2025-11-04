using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Spawn Point")]
    public Transform spawnPoint;

    [Header("Uniek ID voor Netcode")]
    public int checkpointId; // Gebruik dit i.p.v GetInstanceID

    [Header("Visuele instellingen")]
    public Renderer childRenderer; // Sleep hier je child in
    public Color defaultColor = Color.white;
    public Color targetColor = Color.green;

    private bool activated = false; // Houd bij of checkpoint al geactiveerd is

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        // Stel standaardkleur in bij opstart
        if (childRenderer != null)
            childRenderer.material.color = defaultColor;
    }

    // Wordt aangeroepen door PlayerCheckpointDetector of server
    public void Activate()
    {
        if (activated) return; // Niet opnieuw activeren
        activated = true;

        if (childRenderer != null)
            childRenderer.material.color = targetColor;
    }
}
