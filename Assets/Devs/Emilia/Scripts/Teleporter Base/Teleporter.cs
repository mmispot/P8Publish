using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Teleporter : MonoBehaviour
{
    public Transform spawnPoint;   // empty GameObject placed where player should land
    public GameObject destination; // the OTHER teleporter (to disable on arrival)
    public GameObject player;
    [Tooltip("All listed quests must be completed before this teleporter activates.")]
    public SennaQuestData[] requiredCompletedQuests;

    public float cooldown = 2f;
    private static float lastTeleportTime = -Mathf.Infinity;

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player && Time.time > lastTeleportTime + cooldown)
        {
            if (!QuestsCleared()) return;
            if (spawnPoint == null) { Debug.LogError("spawnPoint not assigned on " + gameObject.name); return; }
            if (destination == null) { Debug.LogError("destination not assigned on " + gameObject.name); return; }

            Debug.Log("Teleporter hit: " + gameObject.name + " → sending to " + spawnPoint.position);
            lastTeleportTime = Time.time;

            // The player is NavMeshAgent-driven (SennaPlayerMovement syncs the
            // transform to the agent every frame), so writing transform.position
            // alone gets snapped back to wherever the agent was. Warp moves the
            // agent itself; the transform follows it.
            var agent = player.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                if (agent.Warp(spawnPoint.position))
                    player.transform.position = agent.nextPosition;
                else
                    Debug.LogError("Teleporter: no NavMesh at " + spawnPoint.position
                        + " — bake NavMesh at the destination or move the spawnPoint onto it.");
            }
            else
            {
                // Agent is off mid-jump; the plain move works and the landing
                // logic in SennaPlayerMovement re-snaps to the NavMesh.
                player.transform.position = spawnPoint.position;
            }
            //StartCoroutine(DisableTemporarily(destination));
        }
    }

    private bool QuestsCleared()
    {
        if (requiredCompletedQuests == null || requiredCompletedQuests.Length == 0) return true;
        var qm = SennaQuestManager.Instance;
        foreach (var quest in requiredCompletedQuests)
            if (quest != null && (qm == null || !qm.IsQuestCompleted(quest)))
                return false;
        return true;
    }

    //private IEnumerator DisableTemporarily(GameObject teleporter)
    //{
    //    teleporter.GetComponent<Collider>().enabled = false;
    //    yield return new WaitForSeconds(cooldown);
    //    teleporter.GetComponent<Collider>().enabled = true;
    //}
}