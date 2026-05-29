using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Settings")]
    public int pointsPerKill = 100;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI killText;
    public TextMeshProUGUI scoreText;

    private float elapsedTime = 0f;
    private int killCount = 0;
    private int rawScore = 0;
    private bool missionActive = true;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (missionActive == true)
        {
            elapsedTime += Time.deltaTime;
        }

        int minutes = (int)(elapsedTime / 60);
        int seconds = (int)(elapsedTime % 60);

        if (seconds < 10)
        {
            timerText.text = minutes + ":0" + seconds;
        }
        else
        {
            timerText.text = minutes + ":" + seconds;
        }
    }

    public void RegisterKill()
    {
        killCount++;
        rawScore += pointsPerKill;
        killText.text = "Kills: " + killCount;
        scoreText.text = "Score: " + rawScore;
    }

    public void PlayerDied()
    {
        missionActive = false;
        scoreText.text = "Final Score: " + rawScore;
    }

    public void EndMission()
    {
        missionActive = false;

        float minutes = elapsedTime / 60f;
        int finalScore;

        if (minutes < 2f)
        {
            finalScore = rawScore * 5;
            scoreText.text = "Final Score: " + finalScore + "  (x5!)";
        }
        else if (minutes < 4f)
        {
            finalScore = rawScore * 3;
            scoreText.text = "Final Score: " + finalScore + "  (x3!)";
        }
        else if (minutes < 6f)
        {
            finalScore = rawScore * 2;
            scoreText.text = "Final Score: " + finalScore + "  (x2!)";
        }
        else
        {
            finalScore = rawScore;
            scoreText.text = "Final Score: " + finalScore + "  (no bonus)";
        }
    }
}