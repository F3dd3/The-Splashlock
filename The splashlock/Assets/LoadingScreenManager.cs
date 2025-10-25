using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class LoadingScreenManager : NetworkBehaviour
{
    public static LoadingScreenManager Instance;

    [Header("UI Elements")]
    [Tooltip("Sleep hier je loading screen image in.")]
    public Image loadingImage;

    [Tooltip("Optionele tekst, bv. 'Loading...'")]
    public TextMeshProUGUI loadingText;

    [Header("Timing")]
    [Tooltip("Aantal seconden dat het loading screen blijft staan nadat iedereen gespawned is.")]
    public float delayAfterSpawn = 1.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
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
    public void ShowLoadingScreenClientRpc(string sceneName)
    {
        SetLoadingScreenActive(true);

        if (loadingText != null)
            loadingText.text = $"Loading {sceneName}...";
    }

    [ClientRpc]
    public void HideLoadingScreenClientRpc()
    {
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(delayAfterSpawn);
        SetLoadingScreenActive(false);
    }
}
