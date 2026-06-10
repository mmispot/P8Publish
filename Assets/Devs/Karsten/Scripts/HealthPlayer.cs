using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public Renderer enemyRenderer;
    private MaterialPropertyBlock propBlock;
    public int maxHealth = 100;
    public int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }
    public int health = 100;

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        currentHealth -= damageAmount;
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