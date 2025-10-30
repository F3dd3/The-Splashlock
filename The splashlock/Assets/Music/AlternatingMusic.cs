using UnityEngine;

public class AlternatingMusic : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clipA;
    public AudioClip clipB;

    private bool playA = true;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        StartCoroutine(PlayAlternating());
    }

    private System.Collections.IEnumerator PlayAlternating()
    {
        while (true)
        {
            // Wissel tussen A en B
            audioSource.clip = playA ? clipA : clipB;
            audioSource.Play();
            playA = !playA;

            // Wacht tot de clip is afgelopen
            yield return new WaitForSeconds(audioSource.clip.length);

            // Voeg een cooldown van 2 seconden toe voordat de volgende clip start
            yield return new WaitForSeconds(2f);
        }
    }
}
