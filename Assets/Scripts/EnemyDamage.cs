using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damage;

    void Start()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) {
            EnemyHealth enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null) {
                enemyHealth.TakeDamage(damage);
            }
        }
    }

    void Update()
    {
        
    }
}
