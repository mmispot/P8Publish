using UnityEngine;

public class DamageOnContact : MonoBehaviour
{
    [SerializeField] private float damageAmount = 25f;
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private LayerMask playerLayer = ~0;

    private BoxCollider _collider;
    private readonly Collider[] _hits = new Collider[4];
    private float _lastDamageTime = -999f;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        if (_collider == null) return;
        if (Time.time - _lastDamageTime < damageCooldown) return;

        Vector3 center = transform.TransformPoint(_collider.center);
        Vector3 halfExtents = Vector3.Scale(_collider.size * 0.5f, transform.lossyScale);

        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _hits, transform.rotation, playerLayer, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            SennaPlayerHealth health = _hits[i].GetComponentInParent<SennaPlayerHealth>();
            if (health == null) continue;

            health.TakeDamage(damageAmount);
            _lastDamageTime = Time.time;
            break;
        }
    }
}
