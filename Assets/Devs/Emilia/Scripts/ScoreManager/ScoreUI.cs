using UnityEngine;
using UnityEngine.UI;
using TMPro;                // remove this line if you use legacy UI Text

public class ScoreUI : MonoBehaviour
{
    [Header("HUD (shown during play)")]
    public TMP_Text liveScoreText;  // e.g. "Score: 9,234"
    public TMP_Text timerText;      // e.g. "00:42"

    [Header("End Screen")]
    public GameObject endScreen;
    public TMP_Text finalScoreText;  // shows the final score
    public TMP_Text finalTimeText;   // shows the final time
    public TMP_Text outcomeText;     // "You Win!" or "You Died!"

    [Header("Buttons")]
    public Button playButton;

    void Start()
    {
        endScreen?.SetActive(false);

        playButton?.onClick.AddListener(() =>
        {
            endScreen?.SetActive(false);
            ScoreManager.Instance.StartTimer();
        });

        ScoreManager.Instance.OnGameEnded += ShowEndScreen;
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGameEnded -= ShowEndScreen;
    }

    void Update()
    {
        if (ScoreManager.Instance == null || !ScoreManager.Instance.IsRunning) return;

        if (liveScoreText != null)
            liveScoreText.text = $"Score: {ScoreManager.Instance.GetLiveScore():N0}";

        if (timerText != null)
        {
            float t = ScoreManager.Instance.ElapsedSeconds;
            timerText.text = $"{Mathf.FloorToInt(t / 60f):00}:{Mathf.FloorToInt(t % 60f):00}";
        }
    }

    void ShowEndScreen(int score, float seconds, bool won)
    {
        endScreen?.SetActive(true);

        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {score:N0}";

        if (finalTimeText != null)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            finalTimeText.text = $"Time: {mins:00}:{secs:00}";
        }

        if (outcomeText != null)
            outcomeText.text = won ? "You Win!" : "You Died!";
    }
}