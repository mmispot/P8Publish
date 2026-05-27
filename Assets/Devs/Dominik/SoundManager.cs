using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//als je ergens moet aaroepen bijvoorbeeld bij een player actie zet dit eronder: SoundManager.PlaySound(SoundType."de enum die je nodig hebt uit de lijst");

public enum SoundType
{
    TEST,
    //JUMP,
    //WALK,
    //DEATH,
    //PEEPER,
    //SQUIDDLE,
    //SPINO,
    //MAIN_THEME,
    //DEATH_THEME,


}

[RequireComponent(typeof(AudioSource))]

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundList;
    private static SoundManager instance;
    private AudioSource audioSource;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], volume);
    }

}
