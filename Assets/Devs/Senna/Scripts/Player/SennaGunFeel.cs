using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

// Bridges the Shoot input to recoil feedback without touching SchootingRaycast's logic.
// Only subscribes to the action — never enables/disables it, so
// GameStateManager's DisableShoot() silences shooting, recoil, and tracers together.
public class SennaGunFeel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponSway weaponSway;
    [SerializeField] private SennaCameraShake cameraShake;
    [SerializeField] private SennaPlayerMovement playerMovement;
    [SerializeField] private SchootingRaycast schootingRaycast;

    [Header("Recoil Feel")]
    [SerializeField] private float recoilStrength = 1f;
    [SerializeField] private float recoilCameraKick = 0.7f;
    [SerializeField] private float shakeTrauma = 0.12f;

    [Header("Muzzle Flash")]
    [SerializeField] private VisualEffect muzzleFlash;

    [Header("Bullet Tracer")]
    [SerializeField] private SennaBulletTracer tracerPrefab; // optional — built in code when empty

    [Header("Fire Animation")]
    // Arms + gun animators. Each gets its "Fire" trigger set on the same frame, so the
    // animations can never drift apart. The controllers route AnyState -> Fire on the trigger.
    [SerializeField] private Animator[] fireAnimators;
    [SerializeField] private string fireTrigger = "Fire";

    private IObjectPool<SennaBulletTracer> _tracerPool;
    private int _fireTriggerHash;

    private void Awake()
    {
        _fireTriggerHash = Animator.StringToHash(fireTrigger);

        if (weaponSway == null)      weaponSway      = GetComponentInChildren<WeaponSway>();
        if (playerMovement == null)  playerMovement  = GetComponentInParent<SennaPlayerMovement>();
        if (schootingRaycast == null) schootingRaycast = GetComponentInParent<SchootingRaycast>();
        if (schootingRaycast == null) schootingRaycast = GetComponentInChildren<SchootingRaycast>();
        if (cameraShake == null && playerMovement != null)
            cameraShake = playerMovement.GetComponentInChildren<SennaCameraShake>();

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
        if (schootingRaycast != null)
            schootingRaycast.onShotFired += OnShotFired;
    }

    private void OnDisable()
    {
        if (schootingRaycast != null)
            schootingRaycast.onShotFired -= OnShotFired;
    }

    private void OnShotFired(Vector3 start, Vector3 end)
    {
        weaponSway?.TriggerRecoil(recoilStrength);
        cameraShake?.TriggerShake(shakeTrauma);
        playerMovement?.AddRecoil(recoilCameraKick);

        muzzleFlash?.Play();
        PlayFireAnimations();

        if (_tracerPool == null) return;
        var tracer = _tracerPool.Get();
        tracer.Initialize(start, end, t => _tracerPool.Release(t));
        SoundManager.PlaySound(SoundType.TOKAREV);
    }

    private void PlayFireAnimations()
    {
        if (fireAnimators == null) return;

        foreach (var animator in fireAnimators)
        {
            if (animator == null || !animator.isActiveAndEnabled) continue;

            // Only fire the trigger on controllers that actually declare it (skips e.g. an
            // idle-only controller) so Unity doesn't log a missing-parameter warning.
            if (!HasParameter(animator, _fireTriggerHash)) continue;
            animator.SetTrigger(_fireTriggerHash);
        }
    }

    private static bool HasParameter(Animator animator, int paramHash)
    {
        foreach (var p in animator.parameters)
            if (p.nameHash == paramHash) return true;
        return false;
    }
}
