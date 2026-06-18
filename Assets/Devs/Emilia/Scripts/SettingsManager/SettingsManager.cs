using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    private Resolution[] _availableResolutions;
    private int _currentResolutionIndex;

    private static readonly Vector2Int DefaultResolution = new Vector2Int(1920, 1080);

    void OnEnable()
    {
        ApplyDefaultResolution();
        PopulateResolutionDropdown();
    }

    // Automatically sets 1920x1080 when the settings panel opens
    private void ApplyDefaultResolution()
    {
        bool isFullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;
        Screen.SetResolution(DefaultResolution.x, DefaultResolution.y, isFullscreen);
    }

    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        _availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        _currentResolutionIndex = 0;

        for (int i = 0; i < _availableResolutions.Length; i++)
        {
            var r = _availableResolutions[i];
            options.Add($"{r.width} x {r.height} @ {r.refreshRateRatio.numerator}Hz");

            if (r.width == DefaultResolution.x && r.height == DefaultResolution.y)
                _currentResolutionIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = _currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void OnResolutionChanged(int index)
    {
        var r = _availableResolutions[index];
        bool isFullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;
        Screen.SetResolution(r.width, r.height, isFullscreen);
    }

    public void OnFullscreenToggleChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void OnClosePressed()
    {
        GameStateManager.Instance.OnSettingsClosed();
    }
}