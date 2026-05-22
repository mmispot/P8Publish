using UnityEngine;

public class WhereWolf : MonoBehaviour
{
    public GameObject CapsuleObject;
    public GameObject targetPosition;
    public int damage;
    public float heavyCooldownTime = 2f;
    public float nextAllowedHitTime = 0f;
    public int heavyDamage = 20;
    public float heavyAttackRange = 4f;
    public GameObject heavyAttackPoint;
    public float ChaseRange = 10f;


    public float speed = 1f;
    void Start()
    {
        heavyAttackPoint.SetActive(false);
    }

    void Update()
    {
        if (targetPosition != null)
        {
            float distancetoPlayer = Vector3.Distance(CapsuleObject.transform.position, targetPosition.transform.position);
            if (distancetoPlayer > heavyAttackRange)
            {
                CapsuleObject.transform.position = Vector3.MoveTowards(
                CapsuleObject.transform.position,
                targetPosition.transform.position,
                speed * Time.deltaTime
                );
            }
        }
        if (targetPosition != null)
        {
            if (nextAllowedHitTime <= Time.time)
            {
                if (Vector3.Distance(CapsuleObject.transform.position, targetPosition.transform.position) <= heavyAttackRange)
                {
                    TurnOnHeavyAttack();
                    Invoke("TurnOffHeavyAttack", 0.1f);
                    nextAllowedHitTime = Time.time + heavyCooldownTime;
                }
            }
        }

    }

    public void TurnOnHeavyAttack()
    {
        //TurnOffLightAttack();
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