using UnityEngine;
using Unity.Netcode;

public class GamePlayerColor : NetworkBehaviour
{
    [Header("Renderer")]
    public Renderer playerRenderer;

    // Networked kleur
    public NetworkVariable<Vector3> playerColor = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (playerRenderer == null)
            playerRenderer = GetComponentInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Update kleur als deze verandert
        playerColor.OnValueChanged += OnColorChanged;

        // Direct toepassen bij join
        if (playerColor.Value != Vector3.zero)
            ApplyColor(playerColor.Value);
    }

    private void OnColorChanged(Vector3 oldColor, Vector3 newColor)
    {
        ApplyColor(newColor);
    }

    private void ApplyColor(Vector3 colVec)
    {
        if (playerRenderer == null) return;
        playerRenderer.material.color = new Color(colVec.x, colVec.y, colVec.z);
    }

    /// <summary>
    /// Alleen de server mag dit aanroepen
    /// </summary>
    public void SetColor(Color color)
    {
        if (!IsServer) return;

        Vector3 colorVec = new Vector3(color.r, color.g, color.b);
        playerColor.Value = colorVec;

        // Forceer kleur bij alle clients
        ForceColorClientRpc(colorVec);
    }

    [ClientRpc]
    private void ForceColorClientRpc(Vector3 colorVec)
    {
        ApplyColor(colorVec);
    }
}
