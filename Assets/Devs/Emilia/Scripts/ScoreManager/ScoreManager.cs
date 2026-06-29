using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    public int baseScore = 100000;
    public float decayConstant = 0.003f;

    //win multiplier
    [Range(0f, 1f)]
    public float winMultiplier = 1.0f;

    //death multiplier
    [Range(0f, 1f)]
    public float deathMultiplier = 0.5f;

    public bool IsRunning { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public int FinalScore { get; private set; }

    public event System.Action<int, float, bool> OnGameEnded; // score, time, won

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (IsRunning)
            ElapsedSeconds += Time.deltaTime;
    }

    public void StartTimer()
    {
        ElapsedSeconds = 0f;
        FinalScore = 0;
        IsRunning = true;
    }

    public void OnPlayerWin() => EndGame(won: true);

    public void OnPlayerDeath() => EndGame(won: false);

    void EndGame(bool won)
    {
        if (!IsRunning) return;
        IsRunning = false;

        float multiplier = won ? winMultiplier : deathMultiplier;
        FinalScore = CalculateScore(ElapsedSeconds, multiplier);

        Debug.Log($"Game ended | Won: {won} | Time: {ElapsedSeconds:F2}s | Score: {FinalScore}");
        OnGameEnded?.Invoke(FinalScore, ElapsedSeconds, won);
    }

    /// <summary>score = baseScore x (1 / (1 + k x t)) x multiplier</summary>
    int CalculateScore(float seconds, float multiplier)
    {
        float raw = baseScore * (1f / (1f + decayConstant * seconds)) * multiplier;
        return Mathf.RoundToInt(raw);
    }

    public int GetLiveScore()
    {
        if (!IsRunning) return FinalScore;
        return CalculateScore(ElapsedSeconds, winMultiplier);
    }
}