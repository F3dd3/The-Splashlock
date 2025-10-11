using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance;

    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Spawnpoints")]
    public Transform[] spawnPoints;

    private readonly List<Color> allColors = new List<Color>
    { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

    private readonly Dictionary<ulong, Color> playerColors = new Dictionary<ulong, Color>();
    private readonly Dictionary<ulong, Player> playerRefs = new Dictionary<ulong, Player>();
    private int nextSpawnIndex = 0;

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

        // Spawn enkel als nog niet gespawned
        if (!playerRefs.ContainsKey(clientId))
            SpawnPlayer(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (playerColors.ContainsKey(clientId)) playerColors.Remove(clientId);
        if (playerRefs.ContainsKey(clientId)) playerRefs.Remove(clientId);
    }

    private Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints.Length == 0) return Vector3.zero;

        Vector3 pos = spawnPoints[nextSpawnIndex % spawnPoints.Length].position;
        nextSpawnIndex++;
        return pos;
    }

    private Color GetNextUniqueColor()
    {
        var usedColors = playerColors.Values.ToList();
        var availableColors = allColors.Except(usedColors).ToList();
        return availableColors.Count > 0 ? availableColors[0] : Random.ColorHSV();
    }

    public void SpawnPlayer(ulong clientId)
    {
        if (playerRefs.ContainsKey(clientId)) return;
        if (playerPrefab == null) return;

        Vector3 spawnPos = GetNextSpawnPosition();
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        Color color = GetNextUniqueColor();
        playerColors[clientId] = color;

        Player playerScript = player.GetComponent<Player>();
        playerRefs[clientId] = playerScript;

        Vector3 colorVec = new Vector3(color.r, color.g, color.b);
        playerScript.SetColorServerRpc(colorVec);
        playerScript.ForceColorClientRpc(colorVec);
    }

    /// <summary>
    /// Reset de spawnpoint index zodat de volgende player weer bij de eerste spawn spawnt.
    /// Dit wordt gebruikt bij Leave Lobby of opnieuw autohost.
    /// </summary>
    public void ResetSpawnPoints()
    {
        nextSpawnIndex = 0;
        Debug.Log("🔄 Spawnpoints gereset. Volgende spawn is weer de eerste spawnpoint.");
    }
}
