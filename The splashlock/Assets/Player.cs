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
        // Abonneren op kleurwijziging
        playerColor.OnValueChanged += OnColorChanged;

        if (IsServer)
        {
            // Alleen de server bepaalt kleur en zet hem
            playerColor.Value = PlayerSpawner.Instance.GetNextUniqueColor();
        }

        // OnValueChanged wordt pas getriggerd bij een echte verandering,
        // dus hier alvast een force-update uitvoeren
        OnColorChanged(Color.white, playerColor.Value);
    }

    private void OnDestroy()
    {
        playerColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        if (targetRenderer != null && targetRenderer.material != null)
        {
            targetRenderer.material.color = newColor;
        }
    }

    private void Start()
    {
        if (IsOwner)
        {
            Debug.Log($"Player {OwnerClientId} ready at position {transform.position}, color: {playerColor.Value}");
        }
    }
}
