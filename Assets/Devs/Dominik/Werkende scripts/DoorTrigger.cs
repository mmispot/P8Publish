using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public Door doorScript;

    private void Update()
    {
        if (doorScript == null)
        {
            Debug.LogError("Door script is not assigned in the inspector.");
            doorScript = GetComponentInParent<Door>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (other.CompareTag("Player"))
        //{
        //    Debug.Log("Player entered the trigger");
        //    doorScript.Open(other.transform);
        //}

        Debug.Log("Player entered the trigger");
        doorScript.Open(other.transform);
    }


    private void OnTriggerExit(Collider other)
    {
        //if (other.CompareTag("Player"))
        //{
        //    Debug.Log("Player exited the trigger");
        //    doorScript.StartClose();
        //}

        Debug.Log("Player exited the trigger");
        doorScript.StartClose();
    }
}