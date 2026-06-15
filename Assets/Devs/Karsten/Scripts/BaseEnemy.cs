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
        //transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
        if (targetPosition != null)
        {
            float distance = Vector3.Distance(CapsuleObject.transform.position, targetPosition.transform.position);

            if (distance <= DetectionRange && distance > lightAttackRange)
            {
                agent.SetDestination(targetPosition.transform.position);
                animator.SetBool("isWalking", true);
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
                TurnOnLightAttack();
                Invoke("TurnOffLightAttack", 0.5f);
                nextAllowedHitTime = Time.time + lightCooldownTime;
            }
            else if (distance > DetectionRange)
            {
                agent.isStopped = true;
                agent.ResetPath();
                animator.SetBool("isWalking", false);
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


    private void Die()
    {
        int randomDeath = Random.Range(1, 4); // picks Death 1, 2, or 3
        animator.Play("Death " + randomDeath);
        Invoke("DestroyEnemy", 2f); 
    }
    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }
    void EyesGlow() 
    { 

    }

}