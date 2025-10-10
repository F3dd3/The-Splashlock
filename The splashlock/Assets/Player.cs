using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    [Header("Renderer")]
    public Renderer playerRenderer;

    public NetworkVariable<Vector3> playerColor = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Start()
    {
        if (playerRenderer == null)
            playerRenderer = GetComponentInChildren<Renderer>();

        ApplyColor(playerColor.Value);

        // Realtime update
        playerColor.OnValueChanged += OnColorChanged;
    }

    private void OnDestroy()
    {
        playerColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Vector3 oldColor, Vector3 newColor)
    {
        ApplyColor(newColor);
    }

    private void ApplyColor(Vector3 colorVec)
    {
        Color color = new Color(colorVec.x, colorVec.y, colorVec.z);
        if (playerRenderer != null)
            playerRenderer.material.color = color;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(Vector3 newColor)
    {
        playerColor.Value = newColor;
    }
}
