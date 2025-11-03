using System.Collections;
using UnityEngine;

public class SequentialMusicPlayer : MonoBehaviour
{
    [Header("Clips om na elkaar af te spelen")]
    public AudioClip[] musicClips;

    [Header("Instellingen")]
    public float delayBetweenClips = 2f;
    public bool loopAll = false;
    public float fadeInDuration = 1f;

    private void Start()
    {
        if (musicClips.Length > 0 && MusicManager.Instance != null)
        {
            StartCoroutine(PlaySequence());
        }
    }

    private IEnumerator PlaySequence()
    {
        int index = 0;

        while (true)
        {
            var clip = musicClips[index];

            // Zet expliciet looping uit
            if (MusicManager.Instance != null && MusicManager.Instance.TryGetComponent(out AudioSource src))
                src.loop = false;

            // Speel nieuwe muziek
            MusicManager.Instance.PlayNewMusic(clip, fadeInDuration);

            // wacht tot clip voorbij is
            yield return new WaitForSecondsRealtime(clip.length);

            // 2 seconden pauze (geen muziek)
            MusicManager.Instance.StopMusic(0.5f);  // zachtjes fade-out tijdens cooldown
            yield return new WaitForSecondsRealtime(delayBetweenClips);

            index++;

            // check of we opnieuw moeten beginnen
            if (index >= musicClips.Length)
            {
                if (loopAll)
                    index = 0;
                else
                    yield break;
            }
        }
    }
}