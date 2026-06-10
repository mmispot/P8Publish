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
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        lightAttackPoint.SetActive(false);
        targetPosition = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (targetPosition != null)
        {
            float distance = Vector3.Distance(CapsuleObject.transform.position, targetPosition.transform.position);

            Debug.Log("Distance: " + distance + " AttackRange: " + lightAttackRange);

            if (distance <= DetectionRange && distance > lightAttackRange)
            {
                agent.SetDestination(targetPosition.transform.position);
                animator.SetBool("isWalking", true);
                animator.SetBool("isAttacking", false);
            }
            else if (distance <= lightAttackRange && nextAllowedHitTime <= Time.time)
            {
                agent.ResetPath();
                animator.SetBool("isWalking", true);
                animator.SetBool("isAttacking", false);
                TurnOnLightAttack();
                Invoke("TurnOffLightAttack", 0.5f);
                nextAllowedHitTime = Time.time + lightCooldownTime;
            }
            else if (distance > DetectionRange)
            {
                agent.ResetPath();
                animator.SetBool("isWalking", true);
                animator.SetBool("isAttacking", false);
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
        animator.SetBool("isAttacking", false);
    }


    private void Die()
    {
        animator.SetTrigger("isDead");
        //Invoke("DestroyEnemy", 2f); alleen als er een death ani komt
    }
    private void DestroyGameObject()
    {
        Destroy(gameObject);
    }
}