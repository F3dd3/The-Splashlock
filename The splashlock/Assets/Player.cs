using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    [Header("MeshRenderer van de speler")]
    public MeshRenderer targetRenderer;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<MeshRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        // Kies materiaal bij spawn
        ApplyUniqueMaterial();
    }

    private void ApplyUniqueMaterial()
    {
        if (targetRenderer == null || targetRenderer.sharedMaterials.Length == 0) return;

        // Vraag de lijst van reeds gebruikte materialen van de Spawner
        var usedIndices = PlayerSpawner.Instance.GetUsedMaterialIndices();

        int total = targetRenderer.sharedMaterials.Length;
        int selectedIndex = -1;

        // Kies een materiaal dat nog niet gebruikt wordt
        for (int i = 0; i < total; i++)
        {
            if (!usedIndices.Contains(i))
            {
                selectedIndex = i;
                break;
            }
        }

        // fallback: als alle materialen gebruikt zijn, kies random
        if (selectedIndex == -1)
            selectedIndex = Random.Range(0, total);

        // Pas het materiaal toe op een **instanced materiaal array**
        Material[] mats = targetRenderer.materials; // maakt automatisch kopie
        mats[0] = targetRenderer.sharedMaterials[selectedIndex];
        targetRenderer.materials = mats;

        // Registreer bij de Spawner dat dit materiaal nu in gebruik is
        PlayerSpawner.Instance.RegisterMaterial(selectedIndex);

        Debug.Log($"[Player {OwnerClientId}] toegewezen materiaal index {selectedIndex}");
    }
}
