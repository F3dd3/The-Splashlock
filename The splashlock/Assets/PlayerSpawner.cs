using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance;

    [Header("Prefab van de speler")]
    public GameObject playerPrefab;

    [Header("Spawnpunten in de scene")]
    public Transform[] spawnPoints;

    // Houd bij welke materiaal indices al gebruikt worden
    private HashSet<int> usedMaterialIndices = new HashSet<int>();
    private int nextSpawnIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        SpawnPlayer(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // TODO: vrijgeven van materiaal als speler disconnect
        // (optioneel, implementatie kan per index of via Player script)
    }

    private Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints.Length == 0) return Vector3.zero;

        Vector3 pos = spawnPoints[nextSpawnIndex % spawnPoints.Length].position;
        nextSpawnIndex++;
        return pos;
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is niet ingesteld!");
            return;
        }

        Vector3 spawnPos = GetNextSpawnPosition();
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }

    // Functie die Player kan aanroepen om de reeds gebruikte materialen te checken
    public HashSet<int> GetUsedMaterialIndices()
    {
        return usedMaterialIndices;
    }

    // Functie die Player kan aanroepen om een materiaal als gebruikt te markeren
    public void RegisterMaterial(int index)
    {
        if (!usedMaterialIndices.Contains(index))
            usedMaterialIndices.Add(index);
    }
}
