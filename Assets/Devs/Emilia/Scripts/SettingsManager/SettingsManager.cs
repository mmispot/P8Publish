using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private Resolution[] _availableResolutions;
    private int _currentResolutionIndex;

    private static readonly Vector2Int DefaultResolution = new Vector2Int(1920, 1080);

    private void Start()
    {
        InitializeResolutions();
        InitializeFullscreen();
    }

    private void InitializeResolutions()
    {
        // Get all resolutions Unity reports for this display,
        // filtered to unique width/height pairs (ignore refresh rate duplicates)
        _availableResolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        HashSet<string> seen = new HashSet<string>();
        int defaultIndex = 0;
        int currentIndex = 0;
        int addedCount = 0;

        foreach (Resolution res in _availableResolutions)
        {
            string label = $"{res.width} x {res.height}";
            if (seen.Contains(label)) continue;
            seen.Add(label);

            options.Add(label);

            // Track the default resolution (1920x1080)
            if (res.width == DefaultResolution.x && res.height == DefaultResolution.y)
                defaultIndex = addedCount;

            // Track the current running resolution
            if (res.width == Screen.currentResolution.width &&
                res.height == Screen.currentResolution.height)
                currentIndex = addedCount;

            addedCount++;
        }

        resolutionDropdown.AddOptions(options);

        // Load saved resolution or fall back to 1920x1080
        _currentResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", defaultIndex);

        // Clamp in case saved index is out of range after a display change
        _currentResolutionIndex = Mathf.Clamp(_currentResolutionIndex, 0, options.Count - 1);

        resolutionDropdown.value = _currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void InitializeFullscreen()
    {
        // Load saved fullscreen state, default to true
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.fullScreen = isFullscreen;
        fullscreenToggle.isOn = isFullscreen;

        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    private void OnResolutionChanged(int index)
    {
        _currentResolutionIndex = index;

        // Parse the selected label back into width/height
        string[] parts = resolutionDropdown.options[index].text.Split('x');
        if (parts.Length != 2) return;

        if (int.TryParse(parts[0].Trim(), out int width) &&
            int.TryParse(parts[1].Trim(), out int height))
        {
            Screen.SetResolution(width, height, Screen.fullScreen);
            PlayerPrefs.SetInt("ResolutionIndex", index);
            PlayerPrefs.Save();
        }
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Call this from a Reset button if you want to restore defaults
    public void ResetToDefaults()
    {
        // Find 1920x1080 in the dropdown
        for (int i = 0; i < resolutionDropdown.options.Count; i++)
        {
            if (resolutionDropdown.options[i].text == $"{DefaultResolution.x} x {DefaultResolution.y}")
            {
                resolutionDropdown.value = i;
                break;
            }
        }

        fullscreenToggle.isOn = true;
    }
}