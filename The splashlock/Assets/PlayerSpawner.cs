using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerSpawner : NetworkBehaviour
{
    public static PlayerSpawner Instance;
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("In-game UI")]
    public TextMeshProUGUI joinMessagesUI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Server spawnt een speler en stuurt meldingen
    public void SpawnPlayerServer(ulong clientId)
    {
        if (!IsServer)
        {
            Debug.LogError("SpawnPlayerServer mag alleen op server!");
            return;
        }

        int spawnIndex = GetSpawnIndex(clientId);
        Vector3 spawnPos = spawnPoints[spawnIndex].position;

        GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = playerObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId, true);
        }
        else
        {
            Debug.LogError("Prefab mist NetworkObject component!");
        }

        string label = (clientId == NetworkManager.Singleton.LocalClientId) ? "Host" : $"Speler {spawnIndex}";

        Debug.Log($"{label} gespawned op {spawnPos}");

        // Stuur melding naar host en joiner zelf
        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { NetworkManager.Singleton.LocalClientId, clientId }
            }
        };
        BroadcastSpawnMessageClientRpc(label, rpcParams);
    }

    private int GetSpawnIndex(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) return 0;
        int index = 1;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == clientId) break;
            index++;
        }
        return Mathf.Clamp(index, 1, spawnPoints.Length - 1);
    }

    [ClientRpc]
    private void BroadcastSpawnMessageClientRpc(string playerLabel, ClientRpcParams rpcParams = default)
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.ReceiveSpawnMessage(playerLabel);
        }

        if (joinMessagesUI != null)
        {
            joinMessagesUI.text += $"{playerLabel} heeft het spel joined!\n";
        }
    }
}
