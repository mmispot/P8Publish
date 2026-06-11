using UnityEngine;
using UnityEngine.UI;

public class SennaDamageFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SennaPlayerHealth playerHealth;
    [SerializeField] private SennaCameraShake cameraShake;
    [SerializeField] private Image damageFlashImage;

    [Header("Shake")]
    [SerializeField] private float shakeTrauma = 0.35f;

    [Header("Flash")]
    [SerializeField] private float flashAlpha = 0.35f;
    [SerializeField] private float flashFadeSpeed = 3f;

    private float _currentAlpha;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.onDamaged.AddListener(OnDamaged);
        SetFlashAlpha(0f);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.onDamaged.RemoveListener(OnDamaged);
    }

    private void Update()
    {
        if (_currentAlpha <= 0f) return;

        // Unscaled so the flash still fades out when the death screen freezes time.
        _currentAlpha = Mathf.Max(0f, _currentAlpha - flashFadeSpeed * Time.unscaledDeltaTime);
        SetFlashAlpha(_currentAlpha);
    }

    private void OnDamaged(float remainingHealth)
    {
        if (cameraShake != null)
            cameraShake.TriggerShake(shakeTrauma);

        _currentAlpha = flashAlpha;
        SetFlashAlpha(_currentAlpha);
    }

    private void SetFlashAlpha(float alpha)
    {
        if (damageFlashImage == null) return;
        Color c = damageFlashImage.color;
        c.a = alpha;
        damageFlashImage.color = c;
    }
}
