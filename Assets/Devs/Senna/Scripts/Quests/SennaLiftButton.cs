using UnityEngine;
using UnityEngine.AI;

namespace P8Publish.Quests
{
    // Place on the lift button alongside SennaInteractable (singleUse = true).
    // Wire SennaInteractable.onInteracted -> SennaLiftButton.Teleport in the inspector.
    public class SennaLiftButton : MonoBehaviour
    {
        [SerializeField] private GameObject player;
        [SerializeField] private Transform spawnPoint;

        public void Teleport()
        {
            if (spawnPoint == null) { Debug.LogError("SennaLiftButton: spawnPoint not assigned.", this); return; }
            if (player == null)     { Debug.LogError("SennaLiftButton: player not assigned.", this); return; }

            var agent = player.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                if (!agent.Warp(spawnPoint.position))
                    Debug.LogError("SennaLiftButton: no NavMesh at spawnPoint — bake NavMesh at the destination.", this);
            }
            else
            {
                player.transform.position = spawnPoint.position;
            }
        }
    }
}
