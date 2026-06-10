using UnityEngine;
using UnityEngine.UI;

public class SennaHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SennaPlayerHealth playerHealth;
    [SerializeField] private Image fillImage;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 8f;

    private void OnEnable()
    {
        if (playerHealth == null || fillImage == null) return;
        fillImage.fillAmount = TargetFill();
    }

    private void Update()
    {
        if (playerHealth == null || fillImage == null) return;

        // Poll instead of listening to events: the player starts inactive until
        // Start is pressed, so polling stays correct regardless of init order.
        float target = TargetFill();
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, target, smoothSpeed * Time.unscaledDeltaTime);
        if (Mathf.Abs(fillImage.fillAmount - target) < 0.001f)
            fillImage.fillAmount = target;
    }

    private float TargetFill()
    {
        return playerHealth.MaxHealth > 0f ? playerHealth.CurrentHealth / playerHealth.MaxHealth : 0f;
    }
}
