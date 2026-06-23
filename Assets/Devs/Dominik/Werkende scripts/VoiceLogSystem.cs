using UnityEngine;
using TMPro;

public class VoiceLogSystem : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Header("UI")]
    public GameObject subtitleObject;
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
            subtitleObject.SetActive(false);
            isPlaying = false;
            index = 0;
            return;
        }

        float time = audioSource.time;

        while (index < subtitles.Length && time > subtitles[index].endTime)
        {
            index++;
        }

        if (index < subtitles.Length)
        {
            SubtitleLine line = subtitles[index];

            if (time >= line.startTime && time <= line.endTime)
            {
                subtitleObject.SetActive(true);
                subtitleText.text = line.text;
            }
        }
    }


    public void PlayLog()
    {
        if (isPlaying)
            return;

        index = 0;
        isPlaying = true;

        subtitleObject.SetActive(true);
        audioSource.Play();
    }
}