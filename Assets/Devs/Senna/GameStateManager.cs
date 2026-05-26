using UnityEngine;
using UnityEngine.InputSystem;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject confirmPanel;

    public GameObject playerActive;

    private bool _playing;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        playerActive.SetActive(false);
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

        playerActive.SetActive(true);
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

    public void OnStopPlayingPressed()
    {
        pausePanel.SetActive(false);
        confirmPanel.SetActive(true);
    }

    public void OnConfirmYesPressed() => Application.Quit();

    public void OnConfirmNoPressed()
    {
        confirmPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void OnContinuePressed() => OnStartPressed();

    public void OnSettingsPressed() => Debug.Log("Settings not yet implemented");

    public void OnCreditsPressed() => Debug.Log("Credits not yet implemented");

    public void OnQuitPressed() => Application.Quit();
}
