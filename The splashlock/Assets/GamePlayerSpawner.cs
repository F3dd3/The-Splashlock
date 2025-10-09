using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GamePlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawnpoints in Scene")]
    public Transform[] spawnPoints;

    private Dictionary<ulong, int> clientSpawnIndex = new Dictionary<ulong, int>();

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (sceneName != "GameScene") return;

        Debug.Log("[Server] Alle clients hebben GameScene geladen. Spawning players...");

        int spawnCounter = 0;
        foreach (ulong clientId in clientsCompleted)
        {
            clientSpawnIndex[clientId] = spawnCounter++;
            SpawnPlayerForClient(clientId, clientSpawnIndex[clientId]);
        }
    }

    private void SpawnPlayerForClient(ulong clientId, int spawnIndex)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab niet ingesteld!");
            return;
        }

        Transform spawn = spawnPoints.Length > 0 ? spawnPoints[spawnIndex % spawnPoints.Length] : new GameObject("DummySpawn").transform;
        Quaternion spawnRot = spawn.rotation * Quaternion.Euler(0f, 180f, 0f);
        GameObject player = Instantiate(playerPrefab, spawn.position, spawnRot);

        // Zet spawnpoint info in Die component
        Die dieComp = player.GetComponent<Die>();
        if (dieComp != null)
            dieComp.respawnPoint = spawn;

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.SpawnAsPlayerObject(clientId, true);
        else
            Debug.LogError("Player prefab mist NetworkObject component!");

        // Camera setup
        CameraMovement cam = player.GetComponentInChildren<CameraMovement>();
        if (cam != null)
            cam.SetOwnerCamera(clientId == NetworkManager.Singleton.LocalClientId);

        CharacterMovement cm = player.GetComponent<CharacterMovement>();
        if (cm != null && cam != null)
            cm.SetCamera(cam.transform);
    }
}
