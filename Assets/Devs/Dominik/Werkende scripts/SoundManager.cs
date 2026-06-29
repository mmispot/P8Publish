using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum SoundType
{
    TOKAREV,
    JUMP,
    ENEMYWALK,
    ENEMYDEATH,
    PEEPER,
    ENEMYAGRO,
}

[System.Serializable]
public class SoundEntry
{
    public SoundType soundType;
    public AudioClip[] clips;
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundEntry[] soundList;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup enemiesMixerGroup;
    [SerializeField] private AudioMixerGroup gunsMixerGroup;
    // Add more groups here as needed

    private static SoundManager instance;
    private AudioSource defaultAudioSource;
    private AudioSource enemiesAudioSource;
    private AudioSource gunsAudioSource;

    private Dictionary<SoundType, AudioClip[]> soundDictionary;

    // Define which SoundTypes belong to which group
    private static readonly HashSet<SoundType> enemySounds = new()
    {
        SoundType.ENEMYWALK,
        SoundType.ENEMYDEATH,
        SoundType.PEEPER,
        SoundType.ENEMYAGRO,
    };

    private static readonly HashSet<SoundType> gunSounds = new()
    {
        SoundType.TOKAREV,
    };

    private void Awake()
    {
        instance = this;

        soundDictionary = new Dictionary<SoundType, AudioClip[]>();
        foreach (var entry in soundList)
            soundDictionary[entry.soundType] = entry.clips;

        // The original AudioSource becomes the default (Master)
        defaultAudioSource = GetComponent<AudioSource>();

        // Create extra AudioSources for each mixer group
        enemiesAudioSource = CreateAudioSource(enemiesMixerGroup);
        gunsAudioSource = CreateAudioSource(gunsMixerGroup);
    }

    private AudioSource CreateAudioSource(AudioMixerGroup mixerGroup)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = mixerGroup;
        return source;
    }

    private AudioSource GetAudioSource(SoundType sound)
    {
        if (enemySounds.Contains(sound)) return enemiesAudioSource;
        if (gunSounds.Contains(sound)) return gunsAudioSource;
        return defaultAudioSource;
    }

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        if (instance == null)
        {
            Debug.LogWarning("[SoundManager] No SoundManager in scene — add one to hear sounds.");
            return;
        }

        if (instance.soundDictionary.TryGetValue(sound, out AudioClip[] clips))
        {
            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            instance.GetAudioSource(sound).PlayOneShot(randomClip, volume);
        }
        else
        {
            Debug.LogWarning($"Sound niet gevonden: {sound}");
        }
    }
}