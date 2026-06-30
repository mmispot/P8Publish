using UnityEngine;
using TMPro;
using UnityEngine.Audio;
using System.Collections.Generic;

public class VoiceLogSystem : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Header("UI")]
    public GameObject subtitleObject;
    public TextMeshProUGUI subtitleText;

    [Header("Subtitles")]
    public SubtitleLine[] subtitles;

    [Header("Mixer Groups")]
    public AudioMixerGroup MusicMixerGroup;
    public AudioMixerGroup VoicelogMixerGroup;

    private int index = 0;
    private bool isPlaying = false;

    private static VoiceLogSystem instance;
    private AudioSource defaultAudioSource;
    private AudioSource MusicAudioSource;
    private AudioSource VoicelogAudioSource;

    private void Awake()
    {
        instance = this;

        // The original AudioSource becomes the default (Master)
        defaultAudioSource = GetComponent<AudioSource>();

        // Create extra AudioSources for each mixer group
        MusicAudioSource = CreateAudioSource(MusicMixerGroup);
        VoicelogAudioSource = CreateAudioSource(VoicelogMixerGroup);
    }

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

    private AudioSource CreateAudioSource(AudioMixerGroup mixerGroup)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = mixerGroup;
        return source;
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