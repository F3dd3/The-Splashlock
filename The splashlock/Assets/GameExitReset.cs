using UnityEngine;
using Unity.Netcode;

public class GameExitManager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        ResetAllStaticInstances();
        ResetAllNetworkObjects();
        Debug.Log("[GameExitManager] Alles volledig gereset voor nieuwe run.");
    }

    private void ResetAllStaticInstances()
    {
        // PlayerSpawner
        if (PlayerSpawner.Instance != null)
        {
            PlayerSpawner.Instance.ResetAll();
            PlayerSpawner.Instance = null;
        }

        // Back
        if (Back.Instance != null)
        {
            Back.Instance.FullReset();
            Back.Instance = null;
        }

        // GamePlayerSpawner
        GamePlayerSpawner spawner = FindObjectOfType<GamePlayerSpawner>();
        if (spawner != null)
            spawner.FullReset();
    }

    private void ResetAllNetworkObjects()
    {
        // Stop NetworkManager
        if (NetworkManager.Singleton != null)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    NetworkObject netObj = client.PlayerObject.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned)
                        netObj.Despawn(true);
                    Destroy(client.PlayerObject.gameObject);
                }
            }

            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }

        // Alle andere NetworkObjects in scene
        NetworkObject[] netObjs = FindObjectsOfType<NetworkObject>();
        foreach (var obj in netObjs)
        {
            if (obj != null && obj.IsSpawned)
                obj.Despawn(true);
            if (obj != null)
                Destroy(obj.gameObject);
        }
    }
}
