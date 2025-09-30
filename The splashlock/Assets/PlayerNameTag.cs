using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerNameTag : NetworkBehaviour
{
    public TextMeshPro nameText; // 3D TextMeshPro boven hoofd
    public NetworkVariable<string> playerName = new NetworkVariable<string>("");

    public override void OnNetworkSpawn()
    {
        // Update de naam direct bij spawn
        UpdateName(playerName.Value);

        // Luister naar veranderingen (van server)
        playerName.OnValueChanged += (oldValue, newValue) => UpdateName(newValue);
    }

    private void UpdateName(string newName)
    {
        if (nameText != null)
            nameText.text = newName;
    }
}
