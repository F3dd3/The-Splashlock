using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerBroadcaster : NetworkBehaviour
{
    public static PlayerBroadcaster Instance;

    [SerializeField] private TextMeshProUGUI broadcastText;
    private float broadcastInterval = 2f;
    private float timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsServer) return; // Alleen host broadcast
        timer += Time.deltaTime;
        if (timer >= broadcastInterval)
        {
            BroadcastPlayers();
            timer = 0f;
        }
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
                players.Add($"Speler {++counter}");
        }

        BroadcastMessageClientRpc(string.Join(", ", players));
    }

    [ClientRpc]
    private void BroadcastMessageClientRpc(string message)
    {
        if (broadcastText != null)
        {
            broadcastText.text = "In game: " + message;
        }

        Debug.Log("Broadcast ontvangen: " + message);
    }
}
