using UnityEngine;
using System.Collections;

public class Teleporter : MonoBehaviour
{
    public Transform spawnPoint;   // empty GameObject placed where player should land
    public GameObject destination; // the OTHER teleporter (to disable on arrival)
    public GameObject player;

    public float cooldown = 2f;
    private static float lastTeleportTime = -Mathf.Infinity;

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player && Time.time > lastTeleportTime + cooldown)
        {
            if (spawnPoint == null) { Debug.LogError("spawnPoint not assigned on " + gameObject.name); return; }
            if (destination == null) { Debug.LogError("destination not assigned on " + gameObject.name); return; }

            Debug.Log("Teleporter hit: " + gameObject.name + " → sending to " + spawnPoint.position);
            lastTeleportTime = Time.time;

            player.transform.position = spawnPoint.position;
            //StartCoroutine(DisableTemporarily(destination));
        }
    }

    //private IEnumerator DisableTemporarily(GameObject teleporter)
    //{
    //    teleporter.GetComponent<Collider>().enabled = false;
    //    yield return new WaitForSeconds(cooldown);
    //    teleporter.GetComponent<Collider>().enabled = true;
    //}
}