using UnityEngine;
using System.Collections;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Prefab om te spawnen")]
    public GameObject objectToSpawn;

    [Header("Spawn locatie")]
    public Transform spawnPoint;

    [Header("Spawn instellingen")]
    public int aantalOmTeSpawnen = 5;            // Hoeveel objecten in totaal
    public float minTijdTussenSpawns = 0.5f;     // Minimale tijd tussen spawns
    public float maxTijdTussenSpawns = 2f;       // Maximale tijd tussen spawns

    private void Start()
    {
        StartCoroutine(SpawnObjectsRoutine());
    }

    private IEnumerator SpawnObjectsRoutine()
    {
        for (int i = 0; i < aantalOmTeSpawnen; i++)
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
            Instantiate(objectToSpawn, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogWarning("Prefab of spawnPoint niet ingesteld!");
        }
    }
}