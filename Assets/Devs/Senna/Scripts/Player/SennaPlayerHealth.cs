using UnityEngine;
using UnityEngine.Events;

public class SennaPlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Events")]
    public UnityEvent<float> onDamaged;   // passes remaining health
    public UnityEvent<float> onHealed;    // passes remaining health
    public UnityEvent onDeath;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        onDamaged.Invoke(currentHealth);

        if (currentHealth == 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealed.Invoke(currentHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    private void Die()
    {
        onDeath.Invoke();
    }
}
