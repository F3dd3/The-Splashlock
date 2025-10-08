using UnityEngine;
using Unity.Netcode;

public class CoinManager : NetworkBehaviour
{
    public GameObject coinPrefab;
    public Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // Alleen server spawnt coins

        foreach (var spawn in spawnPoints)
        {
            GameObject coin = Instantiate(coinPrefab, spawn.position, spawn.rotation);
            coin.GetComponent<NetworkObject>().Spawn();
        }
    }
}
