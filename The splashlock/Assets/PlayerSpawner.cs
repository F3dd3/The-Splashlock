using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance;

    [Header("Prefabs & Spawn Points")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Player Colors")]
    public readonly List<Color> allColors = new List<Color>
        { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

    public readonly Dictionary<ulong, Color> playerColors = new Dictionary<ulong, Color>();
    public readonly Dictionary<ulong, Player> playerRefs = new Dictionary<ulong, Player>();
    private readonly Dictionary<ulong, int> playerSpawnIndices = new Dictionary<ulong, int>();

    private int nextSpawnIndex = 1; // host = 0
    private int nextColorIndex = 1; // host = 0

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        SpawnPlayer(clientId, true);

        // Forceer correcte spawnpoint en kleur bij alle clients
        foreach (var kvp in playerRefs)
        {
            ulong id = kvp.Key;
            Player p = kvp.Value;

            if (playerColors.TryGetValue(id, out Color color))
            {
                Vector3 colorVec = new Vector3(color.r, color.g, color.b);
                p.ForceColorClientRpc(colorVec);
            }

            if (playerSpawnIndices.TryGetValue(id, out int index))
            {
                p.transform.position = spawnPoints[index].position;
                p.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        RemovePlayer(clientId);
    }

    public void SpawnPlayer(ulong clientId, bool isMainPlayer = false)
    {
        if (!NetworkManager.Singleton.IsListening) return;
        if (playerPrefab == null) return;

        int spawnIndex;
        Color color;

        bool isNewClient = !playerSpawnIndices.ContainsKey(clientId);

        if (isNewClient)
        {
            if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
            {
                spawnIndex = 0;
                color = allColors[0];
            }
            else
            {
                spawnIndex = nextSpawnIndex % spawnPoints.Length;
                color = allColors[nextColorIndex % allColors.Count];

                nextSpawnIndex++;
                nextColorIndex++;
            }

            playerSpawnIndices[clientId] = spawnIndex;
            playerColors[clientId] = color;
        }
        else
        {
            spawnIndex = playerSpawnIndices[clientId];
            color = playerColors[clientId];
        }

        Vector3 spawnPos = spawnPoints[spawnIndex].position;
        GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
        var netObj = playerObj.GetComponent<NetworkObject>();

        if (isMainPlayer)
            netObj.SpawnAsPlayerObject(clientId, true);
        else
            netObj.Spawn();

        Player playerScript = playerObj.GetComponent<Player>();
        if (isNewClient)
            playerRefs[clientId] = playerScript;

        Vector3 colorVec2 = new Vector3(color.r, color.g, color.b);
        playerScript.SetColorServerRpc(colorVec2);
        playerScript.ForceColorClientRpc(colorVec2);

        // Forceer spawnpoint
        playerScript.transform.position = spawnPos;
        playerScript.transform.rotation = Quaternion.Euler(0, 180, 0);

        if (clientId == NetworkManager.Singleton.LocalClientId && playerScript.nameLabel != null)
            playerScript.nameLabel.text = "You";
    }

    public void RemovePlayer(ulong clientId)
    {
        if (playerRefs.ContainsKey(clientId))
        {
            var netObj = playerRefs[clientId].GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
            playerRefs.Remove(clientId);
        }

        if (playerColors.ContainsKey(clientId)) playerColors.Remove(clientId);
        if (playerSpawnIndices.ContainsKey(clientId)) playerSpawnIndices.Remove(clientId);
    }

    // <-- HIER IS DE NIEUWE RESETALL() METHODE
    public void ResetAll()
    {
        nextSpawnIndex = 1; // host = 0
        nextColorIndex = 1; // host = 0
        playerSpawnIndices.Clear();
        playerColors.Clear();
        playerRefs.Clear();
    }
}
