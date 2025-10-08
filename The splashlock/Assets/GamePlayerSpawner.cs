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

    private int nextSpawnIndex = 0;

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

        if (sceneName == "GameScene")
        {
            Debug.Log("[Server] Alle clients hebben GameScene geladen. Spawning players...");
            foreach (ulong clientId in clientsCompleted)
                SpawnPlayerForClient(clientId);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab niet ingesteld!");
            return;
        }

        Transform spawn = GetNextSpawnPoint();
        Quaternion spawnRot = spawn.rotation * Quaternion.Euler(0f, 180f, 0f);
        GameObject player = Instantiate(playerPrefab, spawn.position, spawnRot);

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId, true);
        }
        else
        {
            Debug.LogError("Player prefab mist NetworkObject component!");
        }

        // Alleen owner camera activeren
        CameraMovement cam = player.GetComponentInChildren<CameraMovement>();
        if (cam != null)
            cam.SetOwnerCamera(clientId == NetworkManager.Singleton.LocalClientId);

        // Zet cameraTransform in CharacterMovement
        CharacterMovement cm = player.GetComponent<CharacterMovement>();
        if (cm != null && cam != null)
            cm.SetCamera(cam.transform);
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("Geen spawnpoints ingesteld. Dummy spawnpoint wordt gebruikt.");
            GameObject dummy = new GameObject("SpawnPoint");
            return dummy.transform;
        }

        Transform t = spawnPoints[nextSpawnIndex % spawnPoints.Length];
        nextSpawnIndex++;
        return t;
    }
}
