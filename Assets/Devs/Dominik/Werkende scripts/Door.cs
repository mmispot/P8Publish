using UnityEngine;

public class Door : MonoBehaviour
{
    public float openAngle = 90f;
    public float speed = 5f;

    private Quaternion closedRot;
    private Quaternion targetRot;

    void Start()
    {
        closedRot = transform.localRotation;
        targetRot = closedRot;
    }

    void Update()
    {
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRot,
            Time.deltaTime * speed
        );
    }

    public void Open(Transform player)
    {
        Vector3 dir = player.position - transform.position;

        float side = Vector3.Dot(transform.forward, dir);

        float angle = side > 0 ? openAngle : -openAngle;

        targetRot = closedRot * Quaternion.Euler(0, angle, 0);
    }

    public void Close()
    {
        targetRot = closedRot;
    }
}