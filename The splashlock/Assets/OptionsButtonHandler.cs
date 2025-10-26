using UnityEngine;
using UnityEngine.UI;

public class OptionsButtonHandler : MonoBehaviour
{
    public Button optionsButton;
    public OptionsManager optionsManager;

    private void Start()
    {
        optionsButton.onClick.AddListener(() =>
        {
            // Alleen openen, niet toggle
            optionsManager.OpenOptions();
        });
    }
}
