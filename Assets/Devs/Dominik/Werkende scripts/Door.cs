using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    public float openAngle = 90f;
    public float speed = 5f;
    public float closeDelay = 1f;

    private Quaternion closedRot;
    private Quaternion targetRot;

    private Coroutine closeCoroutine;

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
        // cancel closing if player comes back
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        Vector3 dir = player.position - transform.position;

        float side = Vector3.Dot(transform.forward, dir);

        float angle = side > 0 ? openAngle : -openAngle;

        targetRot = closedRot * Quaternion.Euler(0, angle, 0);
    }


    public void StartClose()
    {
        if (closeCoroutine != null)
            StopCoroutine(closeCoroutine);

        closeCoroutine = StartCoroutine(CloseAfterDelay());
    }


    IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);

        targetRot = closedRot;
    }
}