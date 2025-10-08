using UnityEngine;

public class GamePlayerSpawner_Local : MonoBehaviour
{
    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawnpoints in Scene")]
    public Transform[] spawnPoints;

    private int nextSpawnIndex = 0;

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab niet ingesteld!");
            return;
        }

        SpawnLocalPlayer();
    }

    private void SpawnLocalPlayer()
    {
        Transform spawn = GetNextSpawnPoint();

        // 180 graden draaien bij spawn
        Quaternion spawnRot = spawn.rotation * Quaternion.Euler(0f, 180f, 0f);
        GameObject player = Instantiate(playerPrefab, spawn.position, spawnRot);

        // CameraMovement en CharacterMovement koppelen
        CameraMovement_Local cam = player.GetComponentInChildren<CameraMovement_Local>();
        CharacterMovement_Local cm = player.GetComponent<CharacterMovement_Local>();

        if (cm != null && cam != null)
            cm.cameraTransform = cam.transform;
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Geen spawnpoints ingesteld. Spawnt in (0,0,0)");
            GameObject dummy = new GameObject("SpawnPoint");
            dummy.transform.position = Vector3.zero;
            return dummy.transform;
        }

        Transform t = spawnPoints[nextSpawnIndex % spawnPoints.Length];
        nextSpawnIndex++;
        return t;
    }
}
