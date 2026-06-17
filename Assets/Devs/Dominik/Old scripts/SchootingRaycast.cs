using UnityEngine;
using UnityEngine.InputSystem;

public class SchootingRaycast : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float range = 100f;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private int damage = 10;

    [Header("Fire Rate")]
    [SerializeField] private float fireCooldown = 0.3f;

    [Header("Input")]
    [SerializeField] private InputActionReference shootAction;

    [Header("Ammo")]
    // Optional: leave empty and the gun fires as before (other scenes are unaffected).
    [SerializeField] private SennaAmmoSystem ammo;

    // Fired after each raycast with muzzle position and impact/end point.
    // SennaGunFeel listens and spawns the bullet tracer visual.
    public event System.Action<Vector3, Vector3> onShotFired;

    private float _nextFireTime;

    private void OnEnable()
    {
        shootAction?.action.Enable();
        if (shootAction?.action != null)
            shootAction.action.started += OnShoot;
    }

    private void OnDisable()
    {
        if (shootAction?.action != null)
            shootAction.action.started -= OnShoot;
        shootAction?.action.Disable();
    }

    public void DisableShoot() => shootAction?.action.Disable();
    public void EnableShoot()  => shootAction?.action.Enable();

    private void OnShoot(InputAction.CallbackContext ctx) => Shoot();

    private void Shoot()
    {
        if (Time.time < _nextFireTime) return;

        // Empty mag -> no shot, and don't burn the cooldown. (dry-fire click could trigger here later)
        if (ammo != null && !ammo.TryConsume()) return;

        _nextFireTime = Time.time + fireCooldown;

        if (playerCamera == null || firePoint == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 endPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            hit.collider.GetComponent<PlayerHealth>()?.TakeDamage(10);
            Debug.Log($"Hit: {hit.collider.name} | Distance: {hit.distance:F2}m");
            PlayerHealth target = hit.collider.GetComponent<PlayerHealth>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }
        else
        {
            endPoint = ray.origin + ray.direction * range;
            Debug.Log("Shot fired — no hit");
        }

        onShotFired?.Invoke(firePoint.position, endPoint);
    }
}
