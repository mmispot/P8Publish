using UnityEngine;
using UnityEngine.AI;

public class AttackRange : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent agent;
    private Transform playerTarget;

    void Start()
    {
        agent = GetComponentInParent<NavMeshAgent>();
    }

    void Update()
    {
        if (agent != null && playerTarget != null)
        {
            agent.SetDestination(playerTarget.position);
        }
    }
    void OnTriggerEnter(Collider other)
     {
         if (other.CompareTag("Player"))
         {
            playerTarget = other.transform;
         }
     }

    void OnTriggerExit(Collider other)
     {
         if (other.CompareTag("Player"))
         {
            playerTarget = null;
            agent.ResetPath();
         }
     }
}
