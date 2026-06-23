using UnityEngine;
using TMPro;

public class VoiceLogSystem : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Header("UI")]
    public TextMeshProUGUI subtitleText;

    [Header("Subtitles")]
    public SubtitleLine[] subtitles;

    private int index = 0;
    private bool isPlaying = false;

    void Update()
    {
        if (!audioSource.isPlaying)
        {
            subtitleText.text = "";
            index = 0;
            isPlaying = false;
            return;
        }

        isPlaying = true;

        float t = audioSource.time;

        while (index < subtitles.Length && t > subtitles[index].endTime)
        {
            index++;
        }

        if (index < subtitles.Length)
        {
            var line = subtitles[index];

            if (t >= line.startTime && t <= line.endTime)
                subtitleText.text = line.text;
            else
                subtitleText.text = "";
        }
    }

    public void PlayLog()
    {
        if (isPlaying) return;

        index = 0;
        audioSource.time = 0;
        audioSource.Play();
    }
}