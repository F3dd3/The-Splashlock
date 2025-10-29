using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    [Header("Checkpoints Setup")]
    public GameObject startPlatform;
    public List<GameObject> checkpoints = new List<GameObject>();

    [Header("Checkpoint Settings")]
    public string activeTag = "Start";
    public float raycastDistance = 2f; // Hoe ver naar beneden checken

    private GameObject currentCheckpoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (startPlatform != null)
        {
            currentCheckpoint = startPlatform;
            SetCheckpointActive(currentCheckpoint);
        }

        // Alle andere checkpoints untagged
        foreach (GameObject cp in checkpoints)
        {
            if (cp != null && cp != startPlatform)
                SetCheckpointInactive(cp);
        }
    }

    private void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        CheckCheckpoint(player);
    }

    private void CheckCheckpoint(GameObject player)
    {
        Vector3 origin = player.transform.position + Vector3.up * 0.1f;
        Ray ray = new Ray(origin, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            if (checkpoints.Contains(hit.collider.gameObject) || hit.collider.gameObject == startPlatform)
            {
                if (hit.collider.gameObject != currentCheckpoint)
                {
                    ActivateCheckpoint(hit.collider.gameObject, player);
                }
            }
        }

        Debug.DrawRay(origin, Vector3.down * raycastDistance, Color.green);
    }

    public void ActivateCheckpoint(GameObject newCheckpoint, GameObject player)
    {
        if (newCheckpoint == null || player == null) return;

        string oldName = currentCheckpoint != null ? currentCheckpoint.name : "none";

        if (currentCheckpoint != null)
            SetCheckpointInactive(currentCheckpoint);

        currentCheckpoint = newCheckpoint;
        SetCheckpointActive(currentCheckpoint);

        // Spawnpositie van speler updaten
        Die playerDie = player.GetComponent<Die>();
        if (playerDie != null)
        {
            Transform spawnPoint = currentCheckpoint.transform.Find("SpawnPoint");
            if (spawnPoint == null) spawnPoint = currentCheckpoint.transform;

            playerDie.SetSpawnProtection(spawnPoint.position);
        }

        Debug.Log($"Checkpoint veranderd van '{oldName}' naar '{currentCheckpoint.name}'");
    }

    private void SetCheckpointActive(GameObject cp)
    {
        cp.tag = activeTag;
    }

    private void SetCheckpointInactive(GameObject cp)
    {
        cp.tag = "Untagged";
    }
}
