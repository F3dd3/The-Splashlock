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

        // Haal het spawnpoint
        Transform spawn = spawnPoints.Length > 0 ? spawnPoints[spawnIndex % spawnPoints.Length] : new GameObject("DummySpawn").transform;
        Vector3 spawnPos = spawn.position;
        Quaternion spawnRot = spawn.rotation * Quaternion.Euler(0f, 180f, 0f);

        // Instantiate prefab **op de juiste plek voor spawn**
        GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);

        // NetworkObject spawnen
        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId, true);
        }
        else
        {
            Debug.LogError("Player prefab mist NetworkObject component!");
        }

        // Camera setup
        CameraMovement cam = player.GetComponentInChildren<CameraMovement>();
        if (cam != null)
            cam.SetOwnerCamera(clientId == NetworkManager.Singleton.LocalClientId);

        // Zet cameraTransform in CharacterMovement
        CharacterMovement cm = player.GetComponent<CharacterMovement>();
        if (cm != null && cam != null)
            cm.SetCamera(cam.transform);

        // **Spawn protection & position instellen** in Die script
        Die dieScript = player.GetComponent<Die>();
        if (dieScript != null)
            dieScript.SetSpawnProtection(spawnPos);
    }
}
