using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    [Header("Renderer die gekleurd moet worden")]
    public Renderer targetRenderer;

    private NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        playerColor.OnValueChanged += OnColorChanged;

        if (IsServer)
        {
            // Server kiest een unieke kleur voor deze speler
            playerColor.Value = PlayerSpawner.Instance.GetNextUniqueColor();
        }

        // Forceer update van kleur
        OnColorChanged(Color.white, playerColor.Value);
    }

    private void OnDestroy()
    {
        playerColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        if (targetRenderer != null && targetRenderer.material != null)
            targetRenderer.material.color = newColor;
    }

    private void Start()
    {
        if (IsOwner)
        {
            Debug.Log($"Player {OwnerClientId} ready at position {transform.position}, color: {playerColor.Value}");
        }
    }

    public void SetPlayerColor(Color color)
    {
        playerColor.Value = color;
    }

}