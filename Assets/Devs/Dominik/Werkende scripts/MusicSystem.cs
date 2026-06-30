using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class MusicSystem : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Mixer Groups")]
    public AudioMixerGroup MusicMixerGroup;

    private int index = 0;
    private bool isPlaying = false;

    private static MusicSystem instance;
    private AudioSource defaultAudioSource;
    private AudioSource MusicAudioSource;

    private void Awake()
    {
        instance = this;

        // The original AudioSource becomes the default (Master)
        defaultAudioSource = GetComponent<AudioSource>();

        // Create extra AudioSources for each mixer group
        MusicAudioSource = CreateAudioSource(MusicMixerGroup);
    }

    private AudioSource CreateAudioSource(AudioMixerGroup mixerGroup)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = mixerGroup;
        return source;
    }


    public void PlayMusic()
    {
        if (isPlaying)
            return;

        index = 0;
        isPlaying = true;

        audioSource.Play();
    }
}