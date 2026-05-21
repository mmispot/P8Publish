using UnityEngine;
using UnityEngine.InputSystem;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject pausePanel;

    private bool _playing;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        startPanel.SetActive(true);
        pausePanel.SetActive(false);
        Time.timeScale = 0f;
    }

    void Update()
    {
        if (!_playing) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool paused = pausePanel.activeSelf;
            pausePanel.SetActive(!paused);
            Time.timeScale = paused ? 1f : 0f;
        }
    }

    public void OnStartPressed()
    {
        startPanel.SetActive(false);
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        _playing = true;
    }

    public void OnResumePressed()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnMainMenuPressed()
    {
        pausePanel.SetActive(false);
        startPanel.SetActive(true);
        Time.timeScale = 0f;
        _playing = false;
    }

    public void OnQuitPressed() => Application.Quit();
}
