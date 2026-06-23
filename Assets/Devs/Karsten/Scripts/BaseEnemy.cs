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
    private bool hasAgrrod = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        lightAttackPoint.SetActive(false);
        targetPosition = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponent<Animator>();
        Debug.Log("Enemy started, target: " + targetPosition);
    }

    void Update()
    {
        if (targetPosition != null)
        {
            float distance = Vector3.Distance(CapsuleObject.transform.position, targetPosition.transform.position);

            if (distance <= DetectionRange && distance > lightAttackRange)
            {
                agent.SetDestination(targetPosition.transform.position);
                animator.SetBool("isWalking", true);
                animator.SetBool("isAttacking", false);
                if (!hasAgrrod)
                {
                    hasAgrrod = true;
                    PlayRandomAttack();
                }
            }
            else if (distance <= lightAttackRange && nextAllowedHitTime <= Time.time)
            {
                agent.isStopped = true;
                agent.ResetPath();
                animator.SetBool("isWalking", false);
                animator.SetBool("isAttacking", true);
                TurnOnLightAttack();
                Invoke("TurnOffLightAttack", 0.5f);
                nextAllowedHitTime = Time.time + lightCooldownTime;
            }
            else if (distance > DetectionRange)
            {
                agent.isStopped = true;
                agent.ResetPath();
                animator.SetBool("isWalking", false);
                animator.SetBool("isAttacking", false);
                hasAgrrod = false;
            }
        } else
            targetPosition = GameObject.FindGameObjectWithTag("Player");

    }
    private void PlayRandomAttack()
    {
        int randomAttack = Random.Range(1, 4);
        animator.SetInteger("AttackIndex", randomAttack);
        animator.SetTrigger("Attack");
    }

    public void TurnOnLightAttack()
    {
        lightAttackPoint.SetActive(true);
    }
    public void TurnOffLightAttack()
    {
        lightAttackPoint.SetActive(false);
    }


    public void Die()
    {
        enabled = false;
        CancelInvoke();

        animator.SetLayerWeight(1, 0f);
        animator.SetLayerWeight(2, 0f);
        Debug.Log("Die called, setting isAttacking false"); // ADD THIS
        animator.SetBool("isAttacking", false);
        animator.SetBool("isWalking", false);

        int randomDeath = Random.Range(1, 4); 
        animator.SetInteger("DeathIndex", randomDeath);
        animator.SetTrigger("Death");
        Invoke("DestroyEnemy", 5f); 
    }
    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }
    void EyesGlow() 
    { 

    }

}