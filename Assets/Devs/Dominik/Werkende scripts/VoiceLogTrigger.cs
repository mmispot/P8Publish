using UnityEngine;

public class VoiceLogTrigger : MonoBehaviour
{
    public VoiceLogSystem voiceLog;
    public bool playOnce = true;

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (playOnce && hasPlayed)
            return;

        voiceLog.PlayLog();
        hasPlayed = true;
    }
}