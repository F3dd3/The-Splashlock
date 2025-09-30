using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerBroadcaster : NetworkBehaviour
{
    public static PlayerBroadcaster Instance;

    [SerializeField] private TextMeshProUGUI broadcastText;
    [SerializeField] private TextMeshProUGUI localMessageText;

    private float broadcastInterval = 2f;
    private float timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsServer) return;
        timer += Time.deltaTime;
        if (timer >= broadcastInterval)
        {
            BroadcastPlayers();
            timer = 0f;
        }
    }

    public void OnPlayerJoined(ulong clientId, int spawnIndex)
    {
        string playerName = clientId == NetworkManager.ServerClientId ? "Host" : $"Player {spawnIndex}";
        BroadcastPlayers();
        ShowLocalJoinMessage($"{playerName} has joined the game!");
    }

    public void ShowLocalJoinMessage(string message)
    {
        if (localMessageText != null)
            localMessageText.text = message;
        Debug.Log(message);
    }

    private void BroadcastPlayers()
    {
        List<string> players = new List<string>();
        int counter = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == NetworkManager.ServerClientId)
                players.Add("Host");
            else
                players.Add($"Player {++counter}");
        }

        BroadcastMessageClientRpc(string.Join(", ", players));
    }

    [ClientRpc]
    private void BroadcastMessageClientRpc(string message)
    {
        if (broadcastText != null)
            broadcastText.text = "In game: " + message;
    }
}
