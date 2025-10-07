using UnityEngine;
using Unity.Netcode;

public class CoinManager : NetworkBehaviour
{
    public GameObject coinPrefab; // prefab moet een NetworkObject hebben
    public Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        // Alleen de server spawn coins
        if (!IsServer) return;

        foreach (var spawn in spawnPoints)
        {
            GameObject coin = Instantiate(coinPrefab, spawn.position, spawn.rotation);
            coin.GetComponent<NetworkObject>().Spawn();
        }
    }
}
