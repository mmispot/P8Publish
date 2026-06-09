using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public GameObject CapsuleObject;
    public GameObject targetPosition;
    public int damage;
    public float lightCooldownTime = 1f;
    public float nextAllowedHitTime = 0f;
    public int lightDamage = 10;
    public float lightAttackRange = 2f;
    public GameObject lightAttackPoint;
    public float speed = 1f;
    private NavMeshAgent agent;
    public float DetectionRange = 10f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        CapsuleObject.transform.position = Vector3.MoveTowards(
            CapsuleObject.transform.position,
            targetPosition.transform.position,
            speed * Time.deltaTime
        );
        lightAttackPoint.SetActive(false);
        targetPosition = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (targetPosition != null)
        {
            float distancetoPlayer = Vector3.Distance(CapsuleObject.transform.position, targetPosition.transform.position);
            if (distancetoPlayer > lightAttackRange)
            {
                agent.SetDestination(targetPosition.transform.position);
            }
        }
        if (targetPosition != null)
        {
            if (nextAllowedHitTime <= Time.time)
            {
                if (Vector3.Distance(CapsuleObject.transform.position, targetPosition.transform.position) <= lightAttackRange)
                {
                    TurnOnLightAttack();
                    Invoke("TurnOffLightAttack", 0.5f);
                    nextAllowedHitTime = Time.time + lightCooldownTime;
                    agent.ResetPath();      
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


    private void Die()
    {
        Destroy(gameObject);
    }
}