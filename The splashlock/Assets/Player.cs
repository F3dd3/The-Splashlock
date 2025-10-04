using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    [Header("Renderer to color")]
    public Renderer targetRenderer;

    public NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.clear,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer != null)
            targetRenderer.material = new Material(targetRenderer.material);
    }

    public override void OnNetworkSpawn()
    {
        if (playerColor != null)
            playerColor.OnValueChanged += OnColorChanged;

        ApplyColor(playerColor.Value);
    }

    private void OnDestroy()
    {
        if (playerColor != null)
            playerColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        ApplyColor(newColor);
    }

    private void ApplyColor(Color color)
    {
        if (targetRenderer != null && targetRenderer.material != null)
            targetRenderer.material.color = color;
    }
}
