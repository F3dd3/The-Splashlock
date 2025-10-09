using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GamePlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawnpoints in Scene")]
    public Transform[] spawnPoints;

    private Dictionary<ulong, int> clientSpawnIndex = new Dictionary<ulong, int>();
    private bool spawningInProgress = false;

    private void OnEnable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected to server.");
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (sceneName != "GameScene") return;

        if (!spawningInProgress)
            StartCoroutine(SpawnPlayersSequentiallyWithDelay(clientsCompleted));
    }

    private IEnumerator SpawnPlayersSequentiallyWithDelay(List<ulong> clientsCompleted)
    {
        spawningInProgress = true;

        Debug.Log("[Server] Waiting a bit before spawning players...");
        yield return new WaitForSeconds(1.0f); // <<--- wacht tot alles echt geladen is

        Debug.Log("[Server] Spawning players sequentially...");

        for (int i = 0; i < clientsCompleted.Count; i++)
        {
            ulong clientId = clientsCompleted[i];
            int spawnIndex = i % spawnPoints.Length;
            clientSpawnIndex[clientId] = spawnIndex;

            yield return new WaitForSeconds(0.3f); // korte delay tussen spelers
            SpawnPlayerForClient(clientId, spawnIndex);
        }

        spawningInProgress = false;
    }

    private void SpawnPlayerForClient(ulong clientId, int spawnIndex)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("❌ Player prefab niet ingesteld!");
            return;
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogError("❌ Geen spawnpoints ingesteld in inspector!");
            return;
        }

        Transform spawn = spawnPoints[spawnIndex];
        Vector3 spawnPos = spawn.position + Vector3.up * 0.5f; // iets boven platform
        Quaternion spawnRot = spawn.rotation * Quaternion.Euler(0f, 180f, 0f);

        GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);

        NetworkObject netObj = player.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId, true);
            Debug.Log($"✅ Spawned player {clientId} at {spawn.name}");
        }
        else
        {
            Debug.LogError("❌ Player prefab mist NetworkObject component!");
        }

        // Camera setup
        CameraMovement cam = player.GetComponentInChildren<CameraMovement>();
        if (cam != null)
            cam.SetOwnerCamera(clientId == NetworkManager.Singleton.LocalClientId);

        CharacterMovement cm = player.GetComponent<CharacterMovement>();
        if (cm != null && cam != null)
            cm.SetCamera(cam.transform);

        // Spawn protectie aan Die-component doorgeven
        Die die = player.GetComponent<Die>();
        if (die != null)
            die.SetSpawnProtection(spawn.position + Vector3.up * 0.5f);
    }
}
