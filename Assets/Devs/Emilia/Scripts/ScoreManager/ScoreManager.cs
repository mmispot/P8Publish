using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    [Tooltip("Maximum score awarded for an instant finish.")]
    public int baseScore = 100000;

    [Tooltip("Controls how fast the score decays over time. Higher = steeper drop.")]
    public float decayConstant = 0.003f;

    [Tooltip("Multiplier applied on a win.")]
    [Range(0f, 1f)]
    public float winMultiplier = 1.0f;

    [Tooltip("Multiplier applied on a death.")]
    [Range(0f, 1f)]
    public float deathMultiplier = 0.5f;

    // ── State ────────────────────────────────────────────────────────────────
    public bool IsRunning { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public int FinalScore { get; private set; }

    // ── Events ───────────────────────────────────────────────────────────────
    public event System.Action<int, float, bool> OnGameEnded; // score, time, won

    // ── Lifecycle ────────────────────────────────────────────────────────────
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

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Call this when the player clicks Play.</summary>
    public void StartTimer()
    {
        ElapsedSeconds = 0f;
        FinalScore = 0;
        IsRunning = true;
    }

    /// <summary>Call this when the player wins.</summary>
    public void OnPlayerWin() => EndGame(won: true);

    /// <summary>Call this when the player dies / loses.</summary>
    public void OnPlayerDeath() => EndGame(won: false);

    // ── Internal ─────────────────────────────────────────────────────────────

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

    /// <summary>Preview what the score would be right now (useful for a live HUD).</summary>
    public int GetLiveScore()
    {
        if (!IsRunning) return FinalScore;
        return CalculateScore(ElapsedSeconds, winMultiplier);
    }
}