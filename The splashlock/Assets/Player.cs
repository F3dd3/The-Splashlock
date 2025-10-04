using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    [Header("Renderer die gekleurd moet worden")]
    public Renderer targetRenderer;

    // NetworkVariable: read door iedereen, write alleen door server
    public NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.clear, // start zonder kleur
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        // Maak een instanced materiaal zodat elke speler zijn eigen kleur kan krijgen
        if (targetRenderer != null)
            targetRenderer.material = new Material(targetRenderer.material);
    }

    public override void OnNetworkSpawn()
    {
        // Blijf luisteren naar veranderingen
        playerColor.OnValueChanged += OnColorChanged;

        // Force update bij spawn
        ApplyColor(playerColor.Value);
    }

    private void OnDestroy()
    {
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
