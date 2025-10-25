using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class LoadingScreenManager : NetworkBehaviour
{
    public static LoadingScreenManager Instance;

    [Header("UI Elements")]
    [Tooltip("Sleep hier het Image-object in dat je loading screen afbeeldt (bijv. een volledige PNG).")]
    public Image loadingImage;

    [Tooltip("Optionele tekst, bijv. 'Laden...' of de mapnaam.")]
    public TextMeshProUGUI loadingText;

    [Header("Timing")]
    [Tooltip("Aantal seconden dat het loading screen blijft staan nadat iedereen gespawned is.")]
    public float delayAfterSpawn = 1.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Blijft bestaan tussen scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Zorg dat image en tekst uit zijn bij start
        SetLoadingScreenActive(false);
    }

    private void SetLoadingScreenActive(bool active)
    {
        if (loadingImage != null)
            loadingImage.gameObject.SetActive(active);

        if (loadingText != null)
            loadingText.gameObject.SetActive(active);
    }

    [ClientRpc]
    public void ShowLoadingScreenClientRpc(string mapName)
    {
        SetLoadingScreenActive(true);

        if (loadingText != null)
            loadingText.text = $"Laden van {mapName}...";
    }

    [ClientRpc]
    public void HideLoadingScreenClientRpc()
    {
        // Start coroutine zodat we delay kunnen toepassen
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(delayAfterSpawn);
        SetLoadingScreenActive(false);
    }
}
