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
    // Arms + gun animators. Each gets its fire state force-restarted on the same
    // frame, so the animations can never drift apart — no parameters needed.
    [SerializeField] private Animator[] fireAnimators;
    [SerializeField] private string fireStateName = "Fire";
    // The gun's fire clip imports under its FBX take name, so the gun controller's
    // state is called this rather than plain "Fire". Used as a fallback per animator:
    // controllers without either state (e.g. the arms' idle-only controller) are skipped.
    [SerializeField] private string gunFireStateName = "TT33Rig|GunTT33_Fire";

    private IObjectPool<SennaBulletTracer> _tracerPool;
    private int _fireStateHash;
    private int _gunFireStateHash;

    private void Awake()
    {
        _fireStateHash = Animator.StringToHash(fireStateName);
        _gunFireStateHash = Animator.StringToHash(gunFireStateName);

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

            // Pick whichever fire state this animator's controller actually has.
            // No matching state (e.g. the arms' idle-only controller) -> skip, so its
            // current state never gets restarted and pops.
            int stateHash =
                animator.HasState(0, _fireStateHash)    ? _fireStateHash    :
                animator.HasState(0, _gunFireStateHash) ? _gunFireStateHash : 0;

            if (stateHash == 0) continue;

            // CrossFadeInFixedTime with time 0 force-restarts the state even when it's
            // already playing — rapid shots re-kick the animation instead of stacking
            // like SetTrigger would.
            animator.CrossFadeInFixedTime(stateHash, 0f, 0, 0f);
        }
    }
}
