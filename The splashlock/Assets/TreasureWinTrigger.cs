using UnityEngine;
using Unity.Netcode;

public class TreasureWinTrigger : NetworkBehaviour
{
    [Header("Canvas met winscreens")]
    public GameObject winCanvas;

    [Header("Win Screens per speler")]
    public GameObject winScreenP1;
    public GameObject winScreenP2;
    public GameObject winScreenP3;
    public GameObject winScreenP4;

    [Header("Optional: Loserscreen (voor alle anderen)")]
    public GameObject loseScreen;

    private bool hasEnded = false;

    private void OnTriggerEnter(Collider other)
    {
        // Alleen de host beslist
        if (!IsServer || hasEnded) return;

        if (other.CompareTag("Player1")) PlayerWonServerRpc(1, other.GetComponent<NetworkObject>().OwnerClientId);
        else if (other.CompareTag("Player2")) PlayerWonServerRpc(2, other.GetComponent<NetworkObject>().OwnerClientId);
        else if (other.CompareTag("Player3")) PlayerWonServerRpc(3, other.GetComponent<NetworkObject>().OwnerClientId);
        else if (other.CompareTag("Player4")) PlayerWonServerRpc(4, other.GetComponent<NetworkObject>().OwnerClientId);

        hasEnded = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerWonServerRpc(int playerNumber, ulong winnerClientId)
    {
        // Server vertelt iedereen wie de winnaar is
        ShowWinClientRpc(playerNumber, winnerClientId);
    }

    [ClientRpc]
    private void ShowWinClientRpc(int playerNumber, ulong winnerClientId)
    {
        Time.timeScale = 0f;

        // Canvas aanzetten
        if (winCanvas != null)
            winCanvas.SetActive(true);

        // Eerst alles uit
        if (winScreenP1) winScreenP1.SetActive(false);
        if (winScreenP2) winScreenP2.SetActive(false);
        if (winScreenP3) winScreenP3.SetActive(false);
        if (winScreenP4) winScreenP4.SetActive(false);
        if (loseScreen) loseScreen.SetActive(false);

        // Check: ben JIJ de winnaar?
        bool isWinner = (NetworkManager.Singleton.LocalClientId == winnerClientId);

        if (isWinner)
        {
            // Alleen winnaar krijgt zijn specifieke winscreen
            switch (playerNumber)
            {
                case 1: if (winScreenP1) winScreenP1.SetActive(true); break;
                case 2: if (winScreenP2) winScreenP2.SetActive(true); break;
                case 3: if (winScreenP3) winScreenP3.SetActive(true); break;
                case 4: if (winScreenP4) winScreenP4.SetActive(true); break;
            }
            Debug.Log($"Jij (Client {winnerClientId}) hebt gewonnen als Player {playerNumber}!");
        }
        else
        {
            // Andere spelers zien eventueel een lose screen
            if (loseScreen) loseScreen.SetActive(true);
            Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} heeft verloren.");
        }
    }
}
