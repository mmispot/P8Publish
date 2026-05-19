using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    public Renderer enemyRenderer;
    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        enemyRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
        UpdateShader();
    }

    void UpdateShader()
    {
        float bloodValue = 1f - (float)currentHealth / maxHealth;
        enemyRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat("_BloodAmount", bloodValue);
        enemyRenderer.SetPropertyBlock(propBlock);
    }

    private void Die() {
 
        Destroy(gameObject);
    }

    void Update()
    {
        
    }
}
