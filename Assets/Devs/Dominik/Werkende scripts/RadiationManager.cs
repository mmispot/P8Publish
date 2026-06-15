using UnityEngine;
using TMPro;

public class RadiationManager : MonoBehaviour
{
    [Header("Radiation Settings")]
    [SerializeField] private float maxRadiation = 1f;
    [SerializeField] private float decayPerSecond = 0.005f;

    [Header("Stage Thresholds")]
    [SerializeField] private float stage2Threshold = 0.40f;
    [SerializeField] private float stage3Threshold = 0.80f;

    [Header("Stage Damage")]
    [SerializeField] private float stage2DamagePerSecond = 5f;
    [SerializeField] private float stage3DamagePerSecond = 10f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI radiationText;

    public float CurrentRadiation { get; private set; }

    private float _radiationRate = 0f;
    private bool _inZone = false;

    private SennaPlayerHealth _playerHealth;

    private void Awake()
    {
        _playerHealth = GetComponent<SennaPlayerHealth>();
        if (_playerHealth == null)
            Debug.LogWarning("[RadiationManager] No SennaPlayerHealth on this GameObject; radiation will not damage the player.");
    }

    private void Update()
    {
        UpdateRadiation();
        ApplyStageDamage();
    }

    private void UpdateRadiation()
    {
        if (_inZone)
        {
            CurrentRadiation = Mathf.Min(CurrentRadiation + _radiationRate * Time.deltaTime, maxRadiation);
        }
        else
        {
            CurrentRadiation = Mathf.Max(CurrentRadiation - decayPerSecond * Time.deltaTime, 0f);
        }

        if (radiationText != null)
            radiationText.text = $"Radiation: {CurrentRadiation * 100:F0}%";
    }

    private void ApplyStageDamage()
    {
        if (_playerHealth == null) return;

        if (CurrentRadiation >= maxRadiation)
        {
            _playerHealth.TakeDamage(_playerHealth.CurrentHealth);
            return;
        }

        // Pass float damage directly — the old Mathf.RoundToInt rounded
        // sub-frame damage (rate * deltaTime) to 0 every frame, so the stage
        // rates never actually applied. SennaPlayerHealth.TakeDamage takes a float.
        if (CurrentRadiation >= stage3Threshold)
            _playerHealth.TakeDamage(stage3DamagePerSecond * Time.deltaTime);
        else if (CurrentRadiation >= stage2Threshold)
            _playerHealth.TakeDamage(stage2DamagePerSecond * Time.deltaTime);
    }

    public void EnterZone(float ratePerSecond)
    {
        _radiationRate += ratePerSecond;
        _inZone = true;
    }

    public void ExitZone(float ratePerSecond)
    {
        _radiationRate -= ratePerSecond;
        if (_radiationRate <= 0f)
        {
            _radiationRate = 0f;
            _inZone = false;
        }
    }
}