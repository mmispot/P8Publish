using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private GameObject deathPanel;

    public GameObject playerActive;
    [SerializeField] private SennaPlayerMovement playerMovement;
    [SerializeField] private SchootingRaycast shooting;

    private bool _playing;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (playerMovement == null && playerActive != null)
            playerMovement = playerActive.GetComponent<SennaPlayerMovement>();


        playerActive.SetActive(false);
    }

    void Start()
    {
        startPanel.SetActive(true);
        pausePanel.SetActive(false);
        deathPanel?.SetActive(false);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        playerMovement?.EnableMovement();
        playerMovement?.EnableMouseLook();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnStartPressed()
    {
        startPanel.SetActive(false);
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        _playing = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerActive.SetActive(true);
    }

    public void OnResumePressed() => Resume();

    public void OnMainMenuPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnStopPlayingPressed()
    {
        pausePanel.SetActive(false);
        confirmPanel.SetActive(true);
    }

    public void OnConfirmYesPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnConfirmNoPressed()
    {
        confirmPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void OnPlayerDied()
    {
        _playing = false;
        Time.timeScale = 0f;
        playerMovement?.DisableMovement();
        playerMovement?.DisableMouseLook();
        shooting?.DisableShoot();
        deathPanel?.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnRespawnPressed()
    {
        deathPanel?.SetActive(false);
        playerActive.GetComponent<SennaPlayerHealth>()?.ResetHealth();
        Time.timeScale = 1f;
        _playing = true;
        playerMovement?.EnableMovement();
        playerMovement?.EnableMouseLook();
        shooting?.EnableShoot();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnContinuePressed() => OnStartPressed();

    public void OnSettingsPressed() => Debug.Log("Settings not yet implemented");

    public void OnCreditsPressed() => Debug.Log("Credits not yet implemented");

    public void OnQuitPressed() => Application.Quit();
}
