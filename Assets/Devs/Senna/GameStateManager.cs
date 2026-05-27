using UnityEngine;
using UnityEngine.InputSystem;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject confirmPanel;

    public GameObject playerActive;
    [SerializeField] private PlayerMovement playerMovement;

    private bool _playing;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (playerMovement == null && playerActive != null)
            playerMovement = playerActive.GetComponent<PlayerMovement>();

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
            if (pausePanel.activeSelf)
                Resume();
            else
                Pause();
        }
    }

    private void Pause()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        playerMovement?.DisableMovement();
        playerMovement?.DisableMouseLook();
    }

    private void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        playerMovement?.EnableMovement();
        playerMovement?.EnableMouseLook();
    }

    public void OnStartPressed()
    {
        startPanel.SetActive(false);
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        _playing = true;

        playerActive.SetActive(true);
    }

    public void OnResumePressed() => Resume();

    public void OnMainMenuPressed()
    {
        Pause();
        pausePanel.SetActive(false);
        startPanel.SetActive(true);
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
