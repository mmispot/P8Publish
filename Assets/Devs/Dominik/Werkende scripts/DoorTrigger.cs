using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public Door doorScript;

    private void Awake()
    {
        if (doorScript == null)
            doorScript = GetComponentInParent<Door>();

        if (doorScript == null)
            Debug.LogError("DoorTrigger could not find a Door script in parent!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered the trigger");
            doorScript.Open(other.transform);
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player exited the trigger");
            doorScript.StartClose();
        }
    }
}