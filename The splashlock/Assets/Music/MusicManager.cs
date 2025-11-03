using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    [Header("General Settings")]
    [Range(0f, 1f)]
    public float defaultVolume = 1f;
    public float fadeDuration = 1f;

    private void Awake()
    {
        // Singleton: maar 1 MusicManager mag bestaan
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = defaultVolume;

        // Luister naar scene wissels
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            Instance = null;
        }
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        // Kijk of er in de nieuwe scene een SceneMusicConfig staat
        SceneMusicConfig config = FindObjectOfType<SceneMusicConfig>();
        if (config != null && config.musicClip != null)
        {
            PlayNewMusic(config.musicClip, config.fadeInDuration);
        }
    }

    public void PlayNewMusic(AudioClip newClip, float fadeIn = 1f)
    {
        if (newClip == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToNewClip(newClip, fadeIn));
    }

    private IEnumerator FadeToNewClip(AudioClip newClip, float fadeIn)
    {
        if (audioSource.isPlaying)
        {
            // fade out huidige muziek
            float startVol = audioSource.volume;
            for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
                yield return null;
            }
        }

        audioSource.clip = newClip;
        audioSource.Play();

        // fade in nieuwe muziek
        for (float t = 0; t < fadeIn; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, defaultVolume, t / fadeIn);
            yield return null;
        }

        audioSource.volume = defaultVolume;
        fadeCoroutine = null;
    }

    public void StopMusic(float fadeOut = 0.5f)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutAndStop(fadeOut));
    }

    private IEnumerator FadeOutAndStop(float seconds)
    {
        float startVol = audioSource.volume;
        for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVol, 0f, t / seconds);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = defaultVolume;
        fadeCoroutine = null;
    }
}
