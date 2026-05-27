using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SchootingRaycast : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float range = 100f;
    [SerializeField] private LayerMask hitLayers;

    [Header("Debug Line")]
    [SerializeField] private float lineDuration = 0.3f;

    [Header("Input")]
    [SerializeField] private InputActionReference shootAction;

    private LineRenderer lr;

    private void Awake()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.positionCount = 2;
        lr.enabled = false;
    }

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
        if (playerCamera == null || firePoint == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 endPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            Debug.Log($"Hit: {hit.collider.name} | Distance: {hit.distance:F2}m");
        }
        else
        {
            endPoint = ray.origin + ray.direction * range;
            Debug.Log("Shot fired — no hit");
        }

        StopAllCoroutines();
        StartCoroutine(ShowLine(firePoint.position, endPoint));
    }

    private IEnumerator ShowLine(Vector3 start, Vector3 end)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.enabled = true;
        yield return new WaitForSeconds(lineDuration);
        lr.enabled = false;
    }
}
