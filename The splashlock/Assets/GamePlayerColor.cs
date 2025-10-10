using UnityEngine;

public class GamePlayerColor : MonoBehaviour
{
    public Renderer playerRenderer;

    private void Awake()
    {
        if (playerRenderer == null)
            playerRenderer = GetComponentInChildren<Renderer>();
    }

    public void SetColor(Color color)
    {
        if (playerRenderer != null)
        {
            playerRenderer.material.color = color;
        }
    }
}
