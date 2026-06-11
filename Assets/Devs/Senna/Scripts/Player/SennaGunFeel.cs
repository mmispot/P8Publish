using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.VFX;

// Bridges the Shoot input to recoil feedback without touching SchootingRaycast's logic.
// Only subscribes to the action — never enables/disables it, so
// GameStateManager's DisableShoot() silences shooting, recoil, and tracers together.
public class SennaGunFeel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private WeaponSway weaponSway;
    [SerializeField] private SennaCameraShake cameraShake;
    [SerializeField] private SennaPlayerMovement playerMovement;
    [SerializeField] private SchootingRaycast schootingRaycast;

    [Header("Recoil Feel")]
    [SerializeField] private float recoilStrength = 1f;
    [SerializeField] private float recoilCameraKick = 0.7f;
    [SerializeField] private float shakeTrauma = 0.12f;

    [Header("Muzzle Flash")]
    [SerializeField] private VisualEffect muzzleFlash; // optional — auto-found in children when empty

    [Header("Bullet Tracer")]
    [SerializeField] private SennaBulletTracer tracerPrefab; // optional — built in code when empty

    private IObjectPool<SennaBulletTracer> _tracerPool;

    private void Awake()
    {
        if (weaponSway == null)      weaponSway      = GetComponentInChildren<WeaponSway>();
        if (playerMovement == null)  playerMovement  = GetComponentInParent<SennaPlayerMovement>();
        if (schootingRaycast == null) schootingRaycast = GetComponentInParent<SchootingRaycast>();
        if (schootingRaycast == null) schootingRaycast = GetComponentInChildren<SchootingRaycast>();
        if (cameraShake == null && playerMovement != null)
            cameraShake = playerMovement.GetComponentInChildren<SennaCameraShake>();
        if (muzzleFlash == null)
            muzzleFlash = GetComponentInChildren<VisualEffect>();

        // The VFX asset auto-plays on enable — stop it so it only bursts on shots
        muzzleFlash?.Stop();

        _tracerPool = new ObjectPool<SennaBulletTracer>(
            createFunc:      CreateTracer,
            actionOnGet:     t => t.gameObject.SetActive(true),
            actionOnRelease: t => t.gameObject.SetActive(false),
            actionOnDestroy: t => Destroy(t.gameObject),
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize:         16
        );
    }

    private SennaBulletTracer CreateTracer()
    {
        if (tracerPrefab != null)
            return Instantiate(tracerPrefab);

        // No prefab assigned — build one in code. RequireComponent adds the
        // TrailRenderer, and SennaBulletTracer.Awake configures its look.
        var go = new GameObject("BulletTracer");
        return go.AddComponent<SennaBulletTracer>();
    }

    private void OnEnable()
    {
        if (shootAction != null)
            shootAction.action.started += OnShoot;
        if (schootingRaycast != null)
            schootingRaycast.onShotFired += OnShotFired;
    }

    private void OnDisable()
    {
        if (shootAction != null)
            shootAction.action.started -= OnShoot;
        if (schootingRaycast != null)
            schootingRaycast.onShotFired -= OnShotFired;
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        weaponSway?.TriggerRecoil(recoilStrength);
        cameraShake?.TriggerShake(shakeTrauma);
        playerMovement?.AddRecoil(recoilCameraKick);
    }

    private void OnShotFired(Vector3 start, Vector3 end)
    {
        muzzleFlash?.Play();

        if (_tracerPool == null) return;
        var tracer = _tracerPool.Get();
        tracer.Initialize(start, end, t => _tracerPool.Release(t));
    }
}
