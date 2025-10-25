using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Player : NetworkBehaviour
{
    [Header("Visuals")]
    public Renderer playerRenderer;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI readyLabel; // <- voeg dit toe in de inspector

    private void Start()
    {
        // Alleen de lokale speler ziet "You"
        if (IsOwner)
        {
            if (nameLabel != null)
            {
                nameLabel.text = "You";
                nameLabel.gameObject.SetActive(true);
            }
        }
        else
        {
            if (nameLabel != null)
                nameLabel.gameObject.SetActive(false);
        }

        // Ready tekst is standaard uit
        if (readyLabel != null)
            readyLabel.gameObject.SetActive(false);
    }

    public void SetReadyText(bool isReady)
    {
        if (readyLabel != null)
            readyLabel.gameObject.SetActive(isReady);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(Vector3 colorVec)
    {
        Color color = new Color(colorVec.x, colorVec.y, colorVec.z);
        playerRenderer.material.color = color;
        ForceColorClientRpc(colorVec);
    }

    [ClientRpc]
    public void ForceColorClientRpc(Vector3 colorVec, ClientRpcParams clientRpcParams = default)
    {
        Color color = new Color(colorVec.x, colorVec.y, colorVec.z);
        playerRenderer.material.color = color;
    }
}
