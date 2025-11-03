using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class LeaderboardManager : NetworkBehaviour
{
    [Header("UI")]
    public Transform leaderboardParent; // Panel of VerticalLayoutGroup
    public GameObject leaderboardEntryPrefab; // Prefab met TMP_Text

    private List<PlayerProgressSpline> players = new List<PlayerProgressSpline>();
    private Dictionary<ulong, GameObject> entryObjects = new Dictionary<ulong, GameObject>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        players = FindObjectsOfType<PlayerProgressSpline>().ToList();

        foreach (var player in players)
        {
            GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardParent);
            TMP_Text text = entry.GetComponentInChildren<TMP_Text>();
            text.text = player.name;
            entryObjects[player.GetComponent<NetworkObject>().OwnerClientId] = entry;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        players = players.OrderByDescending(p => p.GetProgress()).ToList();

        for (int i = 0; i < players.Count; i++)
        {
            ulong clientId = players[i].GetComponent<NetworkObject>().OwnerClientId;
            if (entryObjects.ContainsKey(clientId))
            {
                entryObjects[clientId].transform.SetSiblingIndex(i);
                TMP_Text text = entryObjects[clientId].GetComponentInChildren<TMP_Text>();
                text.text = $"{i + 1}. {players[i].name}";
            }
        }
    }
}
