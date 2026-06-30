using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    [SerializeField] private MusicSystem music;
    [SerializeField] private bool playOnce = true;

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (music == null)
        {
            Debug.LogError("MusicSystem not assigned!", this);
            return;
        }

        if (!other.CompareTag("Player"))
            return;

        if (playOnce && hasPlayed)
            return;

        music.PlayMusic();
        hasPlayed = true;
    }

    public void ResetTrigger()
    {
        hasPlayed = false;
    }
}