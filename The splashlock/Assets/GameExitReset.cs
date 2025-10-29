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
        // PlayerSpawner volledig resetten
        if (PlayerSpawner.Instance != null)
            PlayerSpawner.Instance.ResetAll();

        // Back volledig resetten
        if (Back.Instance != null)
            Back.Instance.FullReset();

        // GamePlayerSpawner resetten
        GamePlayerSpawner spawner = FindObjectOfType<GamePlayerSpawner>();
        if (spawner != null)
            spawner.FullReset();
    }

    private void ResetAllNetworkObjects()
    {
        // Stop en despawn NetworkManager objecten
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

            // Alleen shutdown, niet destroyen
            NetworkManager.Singleton.Shutdown();
        }

        // Alle andere NetworkObjects in de scene despawnen en destroyen
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
