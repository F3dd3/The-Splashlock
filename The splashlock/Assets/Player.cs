using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
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

        // Voeg callback toe
        playerColor.OnValueChanged += OnColorChanged;

        // Zorg dat ook late joiners de kleur correct zien
        if (playerColor.Value != Vector3.zero)
            ApplyColor(playerColor.Value);

        // Verstuur huidige kleur naar deze client als dit object al een kleur heeft
        if (IsServer)
            ForceColorClientRpc(playerColor.Value, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            });
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
