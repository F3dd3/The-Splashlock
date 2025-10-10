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

    private void Awake()
    {
        if (playerRenderer == null)
            playerRenderer = GetComponentInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        playerColor.OnValueChanged += OnColorChanged;

        // ✅ direct bij join juiste kleur weergeven
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

    private void ApplyColor(Vector3 colorVec)
    {
        if (playerRenderer == null) return;

        Color color = new Color(colorVec.x, colorVec.y, colorVec.z);
        playerRenderer.material.color = color;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetColorServerRpc(Vector3 newColor)
    {
        playerColor.Value = newColor;
    }

    [ClientRpc]
    public void ForceColorClientRpc(Vector3 newColor, ClientRpcParams rpcParams = default)
    {
        ApplyColor(newColor);
    }
}
