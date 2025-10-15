using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Player : NetworkBehaviour
{
    [Header("Visuals")]
    public Renderer playerRenderer;       // Renderer van je player (bijv. body)
    public TextMeshProUGUI nameText;      // TextMeshPro in de Canvas

    private void Start()
    {
        // Alleen lokale speler ziet "you"
        if (IsOwner && nameText != null)
        {
            nameText.text = "you";
            nameText.gameObject.SetActive(true);
        }
        else if (nameText != null)
        {
            // Anderen zien geen tekst boven deze speler
            nameText.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        // Forceer dat de lokale speler altijd "you" ziet
        if (IsOwner && nameText != null && !nameText.gameObject.activeSelf)
        {
            nameText.gameObject.SetActive(true);
            nameText.text = "you";
        }
    }

    // --- Kleuren synchronisatie via ServerRPC & ClientRPC ---

    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(Vector3 colorVec)
    {
        ForceColorClientRpc(colorVec);
    }

    // ClientRpc voor alle clients
    [ClientRpc]
    public void ForceColorClientRpc(Vector3 colorVec)
    {
        ApplyColor(colorVec);
    }

    // ClientRpc voor specifieke clients
    [ClientRpc]
    public void ForceColorClientRpc(Vector3 colorVec, ClientRpcParams clientRpcParams)
    {
        ApplyColor(colorVec);
    }

    // Helper functie om kleur toe te passen
    private void ApplyColor(Vector3 colorVec)
    {
        if (playerRenderer != null)
            playerRenderer.material.color = new Color(colorVec.x, colorVec.y, colorVec.z);
    }
}
