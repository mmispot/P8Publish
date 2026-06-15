using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
   public int Damage;

    void Start()
    {
        
    }

    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (other.gameObject.TryGetComponent<SennaPlayerHealth>(out var playerHealth))
            {
                playerHealth.TakeDamage(Damage);
            }
        }
    }
}
