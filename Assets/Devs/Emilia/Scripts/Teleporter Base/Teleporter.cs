using UnityEngine;

public class Teleporter : MonoBehaviour
{
    public Transform teleporterBase;
    public Transform teleporterMap;

    public GameObject player;

    public float cooldown = 2f;
    private static float lastTeleportTime = -Mathf.Infinity;


    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player && Time.time > lastTeleportTime + cooldown)
        {
            lastTeleportTime = Time.time;

            if (gameObject == teleporterBase)
            {
                Debug.Log("base teleporter hit");
                player.transform.position = teleporterMap.transform.position;
            }
            else if (gameObject == teleporterMap)
            {
                Debug.Log("map teleporter hit");
                player.transform.position = teleporterBase.transform.position;
            }
        }
    }
}
