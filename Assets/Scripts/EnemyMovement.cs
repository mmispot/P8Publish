using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public GameObject CapsuleObject;
    public GameObject targetPosition;
    public int damage;
    public float lightCooldownTime = 1f;
    public float heavyCooldownTime = 2f;
    public float nextAllowedHitTime = 0f;
    public int lightDamage = 10;
    public int heavyDamage = 20;
    public float lightAttackRange = 2f;
    public float heavyAttackRange = 3f;


    public float speed = 1f;
    void Start()
    {

    }

    void Update()
    {
        if (targetPosition != null)
        {
            CapsuleObject.transform.position = Vector3.MoveTowards(
                CapsuleObject.transform.position,
                targetPosition.transform.position,
                speed * Time.deltaTime
                );
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                if (Time.time >= nextAllowedHitTime)
                {
                    enemyHealth.TakeDamage(damage);
                    nextAllowedHitTime = Time.time + lightCooldownTime;
                }
                else
                {
                    enemyHealth.TakeDamage(damage);
                    nextAllowedHitTime = Time.time + heavyCooldownTime;
                }
            }
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

}