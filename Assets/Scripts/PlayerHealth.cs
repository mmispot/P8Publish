using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public Renderer enemyRenderer;
    private MaterialPropertyBlock propBlock;
    [SerializeField] private TextMeshProUGUI healthText;
    public int maxHealth = 100;
    public int currentHealth;

    void Start()
    {
        healthText.text = "Health: " + health.ToString();
        healthText.text = "Health: " + currentHealth.ToString();
        currentHealth = maxHealth;
    }
    void Update()
    {
        healthText.text = "Health: " + health.ToString();
        healthText.text = "Health: " + currentHealth.ToString();
    }
    public int health = 100;

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        Debug.Log("Player took damage! Health is now: " + health);
        currentHealth -= damageAmount;
        Debug.Log("Player took damage! Health is now: " + currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
        UpdateShader();
    }

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    void UpdateShader()
    {
        float bloodValue = (float)currentHealth / maxHealth;
        enemyRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat("_BloodAmount", bloodValue);
        enemyRenderer.SetPropertyBlock(propBlock);
    }
    private void Die()
    {

        Destroy(gameObject);
    }
}