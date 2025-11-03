using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Player))]
public class PlayerHighlight : NetworkBehaviour
{
    private Renderer rend;
    private Player playerScript;
    private Color baseColor;
    private Color highlightColor;
    private bool isHighlighted = false;

    private void Start()
    {
        playerScript = GetComponent<Player>();
        rend = GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            baseColor = rend.material.color;
            highlightColor = baseColor * 1.5f; // maak iets feller
        }
    }

    private void OnMouseEnter()
    {
        if (rend == null || playerScript == null)
            return;

        // Alleen highlighten als dit jouw eigen clone is
        if (playerScript.ownerClientId.Value == NetworkManager.Singleton.LocalClientId)
        {
            rend.material.color = highlightColor;
            isHighlighted = true;
        }
    }

    private void OnMouseExit()
    {
        if (rend == null || !isHighlighted)
            return;

        // Reset kleur als highlight uitgaat
        rend.material.color = baseColor;
        isHighlighted = false;
    }

    // Zorg dat kleur teruggaat naar juiste tint als kleur via Netcode verandert
    private void Update()
    {
        if (playerScript != null && rend != null && !isHighlighted)
        {
            Color netColor = new Color(playerScript.playerColor.Value.x, playerScript.playerColor.Value.y, playerScript.playerColor.Value.z);
            rend.material.color = netColor;
            baseColor = netColor;
            highlightColor = netColor * 1.5f;
        }
    }
}
