using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GamePlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawnpoints in Scene")]
    public Transform[] spawnPoints;

    private int nextSpawnIndex = 0;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Spawn voor alle bestaande clients (host + clients)
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            SpawnPlayerForClient(client.ClientId);
        }

        // Subscribe voor toekomstige clients
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayerForClient;
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab niet ingesteld!");
            return;
        }

        Transform spawn = GetNextSpawnPoint();
        GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation);

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
            cam.gameObject.SetActive(clientId == NetworkManager.Singleton.LocalClientId);

        // Zet cameraTransform in CharacterMovement
        CharacterMovement cm = player.GetComponent<CharacterMovement>();
        if (cm != null && cam != null)
            cm.cameraTransform = cam.transform;
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints.Length == 0)
        {
            GameObject dummy = new GameObject("SpawnPoint");
            return dummy.transform;
        }

        Transform t = spawnPoints[nextSpawnIndex % spawnPoints.Length];
        nextSpawnIndex++;
        return t;
    }
}
