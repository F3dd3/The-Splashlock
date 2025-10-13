using UnityEngine;
using System.Collections;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Prefab om te spawnen")]
    public GameObject objectToSpawn;

    [Header("Spawn locatie")]
    public Transform spawnPoint;

    [Header("Spawn instellingen")]
    public float minTijdTussenSpawns = 0.5f;     // Minimale tijd tussen spawns
    public float maxTijdTussenSpawns = 2f;       // Maximale tijd tussen spawns

    [Header("Object levensduur")]
    public float levensduurObject = 5f;          // Tijd waarna object verdwijnt

    private void Start()
    {
        StartCoroutine(SpawnObjectsRoutine());
    }

    private IEnumerator SpawnObjectsRoutine()
    {
        // Oneindige lus
        while (true)
        {
            SpawnObject();

            float waitTime = Random.Range(minTijdTussenSpawns, maxTijdTussenSpawns);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void SpawnObject()
    {
        if (objectToSpawn != null && spawnPoint != null)
        {
            GameObject nieuwObject = Instantiate(objectToSpawn, spawnPoint.position, spawnPoint.rotation);

            // Vernietig het object na de ingestelde levensduur
            Destroy(nieuwObject, levensduurObject);
        }
        else
        {
            Debug.LogWarning("Prefab of spawnPoint niet ingesteld!");
        }
    }
}