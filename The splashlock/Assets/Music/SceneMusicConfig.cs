using UnityEngine;

public class SceneMusicConfig : MonoBehaviour
{
    [Header("Scene Music Settings")]
    public AudioClip musicClip;
    public float fadeInDuration = 1f;

    private void Start()
    {
        // Start muziek direct bij scene load
        if (MusicManager.Instance != null && musicClip != null)
        {
            MusicManager.Instance.PlayNewMusic(musicClip, fadeInDuration);
        }
    }
}
