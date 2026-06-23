using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    TOKAREV,
    JUMP,
    WALK,
    DEATH,
    PEEPER,
    ENEMYAGRO
}

[System.Serializable]
public class SoundEntry
{
    public SoundType soundType;
    public AudioClip[] clips; //kan meerdere soundeffects hebben
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundEntry[] soundList;

    private static SoundManager instance;
    private AudioSource audioSource;
    private Dictionary<SoundType, AudioClip[]> soundDictionary;

    private void Awake()
    {
        instance = this;

        soundDictionary = new Dictionary<SoundType, AudioClip[]>();

        foreach (var entry in soundList)
        {
            soundDictionary[entry.soundType] = entry.clips;
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        if (instance.soundDictionary.TryGetValue(sound, out AudioClip[] clips))
        {
            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            instance.audioSource.PlayOneShot(randomClip, volume);
        }
        else
        {
            Debug.LogWarning($"Sound niet gevonden: {sound}");
        }
    }
}