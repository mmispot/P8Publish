using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public Door doorScript;
    public float openRange = 2f;

    private Transform _player;
    private bool _isOpen = false;

    private void Awake()
    {
        if (doorScript == null)
            doorScript = GetComponentInParent<Door>();
        if (doorScript == null)
            doorScript = GetComponent<Door>();

        if (doorScript == null)
            Debug.LogError("DoorTrigger could not find a Door script!", this);
    }

    private void Update()
    {
        if (doorScript == null) return;

        // Keep trying to find the player until we have it
        if (_player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) return; // not spawned yet, try next frame
            _player = playerObj.transform;
        }

        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= openRange && !_isOpen)
        {
            _isOpen = true;
            doorScript.Open(_player);
        }
        else if (distance > openRange && _isOpen)
        {
            _isOpen = false;
            doorScript.StartClose();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.pink;
        Gizmos.DrawWireSphere(transform.position, openRange);
    }
}