using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    public Renderer enemyRenderer;
    private MaterialPropertyBlock propBlock;
    public int maxHealth = 100;
    public int currentHealth;
    private Animator animator;
    private EnemyMovement enemymovement;
    private int currentHitIndex = 1;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        enemymovement = GetComponent<EnemyMovement>();
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
        else
        {
            animator.Play("Hit " + currentHitIndex, 2); 
            currentHitIndex++;
            if (currentHitIndex > 5) currentHitIndex = 1;
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
        SennaQuestManager.Instance?.ReportEnemyKilled();
        enemymovement.Die();
    }
}