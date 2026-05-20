using UnityEngine;

public class SchootingRaycast : MonoBehaviour
{
    public Transform FirePoint;

    void Update()
    {
        
    }

    public void Shooting()
    {
        RaycastHit hit;
        if(Physics.Raycast(FirePoint.position , transform.TransformDirection(Vector3.forward) , out hit , 100))
        {
            Debug.DrawRay(FirePoint.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.pink);
        }
    }
}
