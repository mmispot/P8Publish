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
    public GameObject lightAttackPoint;
    public GameObject heavyAttackPoint;


    public float speed = 1f;
    void Start()
    {
        lightAttackPoint.SetActive(false);
        heavyAttackPoint.SetActive(false);
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
        if (targetPosition != null)
        {
            if (nextAllowedHitTime <= Time.time)
            {
                if (Vector3.Distance(CapsuleObject.transform.position, targetPosition.transform.position) <= heavyAttackRange)
                {
                    TurnOnHeavyAttack();
                    Invoke("TurnOffHeavyAttack", 0.5f);
                    nextAllowedHitTime = Time.time + heavyCooldownTime;
                }
                else if (Vector3.Distance(CapsuleObject.transform.position, targetPosition.transform.position) <= lightAttackRange)
                    {
                    TurnOnLightAttack();
                    Invoke("TurnOffLightAttack", 0.5f);
                    nextAllowedHitTime = Time.time + lightCooldownTime;
                }
            }
        }

    }

    public void TurnOnLightAttack()
    {
        lightAttackPoint.SetActive(true);
    }
    public void TurnOffLightAttack()
    {
        lightAttackPoint.SetActive(false);
    }

    public void TurnOnHeavyAttack()
    {
        heavyAttackPoint.SetActive(true);
    }
    public void TurnOffHeavyAttack()
    {
        heavyAttackPoint.SetActive(false);
    }


    private void Die()
    {
        Destroy(gameObject);
    }

}