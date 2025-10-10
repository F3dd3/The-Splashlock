using UnityEngine;
using Unity.Netcode;

public class GamePlayerColor : NetworkBehaviour
{
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
        playerColor.OnValueChanged += OnColorChanged;

        // direct toepassen als waarde al bestaat
        if (playerColor.Value != Vector3.zero)
            ApplyColor(playerColor.Value);
    }

    private void OnDestroy()
    {
        playerColor.OnValueChanged -= OnColorChanged;
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

    // Alleen de server mag dit aanroepen
    public void SetColor(Color color)
    {
        if (!IsServer) return;
        playerColor.Value = new Vector3(color.r, color.g, color.b);
    }
}
