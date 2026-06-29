using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public int Damage;

    [Tooltip("Layers that count as walls and block damage. Set to the Wall/Environment layer.")]
    [SerializeField] private LayerMask wallMask;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        // Block damage if a wall is between the hitbox and the player
        Vector3 origin = transform.position;
        Vector3 target = other.bounds.center;
        Vector3 dir    = target - origin;
        if (wallMask != 0 && Physics.Raycast(origin, dir.normalized, dir.magnitude, wallMask, QueryTriggerInteraction.Ignore))
            return;

        if (other.gameObject.TryGetComponent<SennaPlayerHealth>(out var playerHealth))
            playerHealth.TakeDamage(Damage);
    }
}
