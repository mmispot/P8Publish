using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Drives the reload: on the Reload action (R) it sets the "Reload" trigger on the arms +
// gun animators (controllers route AnyState -> Reload), then refills the magazine once the
// animation finishes. Mirrors SennaGunFeel's approach — both animators are triggered on the
// same frame so the two halves of the reload can never drift apart. Firing is blocked while
// IsReloading is true (SchootingRaycast checks it before each shot).
public class SennaGunReload : MonoBehaviour
{
    [Header("Input")]
    // Self-contained action so this never depends on the shared PlayerControls asset.
    // Rebind it right here in the inspector if R isn't wanted.
    [SerializeField] private InputAction reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");

    [Header("References")]
    // Optional: leave empty and the reload still animates, it just won't refill counts.
    [SerializeField] private SennaAmmoSystem ammo;

    [Header("Reload Animation")]
    // Arms + gun animators — the same two SennaGunFeel fires. Left empty, Awake grabs
    // every Animator under this object so it auto-wires on the player rig.
    [SerializeField] private Animator[] reloadAnimators;
    [SerializeField] private string reloadTrigger = "Reload";

    [Tooltip("Seconds before the magazine refills. Match this to the reload clip length.")]
    [SerializeField] private float reloadDuration = 1.5f;

    public bool IsReloading { get; private set; }

    private int _reloadTriggerHash;
    private Coroutine _reloadRoutine;

    private void Awake()
    {
        _reloadTriggerHash = Animator.StringToHash(reloadTrigger);

        if (ammo == null) ammo = GetComponentInParent<SennaAmmoSystem>();
        if (ammo == null) ammo = GetComponentInChildren<SennaAmmoSystem>();

        if (reloadAnimators == null || reloadAnimators.Length == 0)
            reloadAnimators = GetComponentsInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        reloadAction.started += OnReload;
        reloadAction.Enable();
    }

    private void OnDisable()
    {
        reloadAction.started -= OnReload;
        reloadAction.Disable();

        // Coroutines hold references — stop it and clear the flag so a disabled then
        // re-enabled gun isn't stuck "reloading" and unable to fire.
        if (_reloadRoutine != null)
        {
            StopCoroutine(_reloadRoutine);
            _reloadRoutine = null;
        }
        IsReloading = false;
    }

    private void OnReload(InputAction.CallbackContext ctx)
    {
        if (IsReloading || !CanReload()) return;
        _reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    // Don't animate a pointless reload: mag already full, or no rounds in reserve.
    // With no ammo system wired the reload always plays (count-less test scenes).
    private bool CanReload()
    {
        if (ammo == null) return true;
        return !ammo.IsFull && ammo.ReserveAmmo > 0;
    }

    private IEnumerator ReloadRoutine()
    {
        IsReloading = true;
        PlayReloadAnimations();

        // Scaled wait so a pause (timeScale 0) freezes the timer in step with the
        // animators; the mag fills the moment the clip visually finishes.
        yield return new WaitForSeconds(reloadDuration);

        ammo?.Reload();
        IsReloading = false;
        _reloadRoutine = null;
    }

    private void PlayReloadAnimations()
    {
        if (reloadAnimators == null) return;

        foreach (var animator in reloadAnimators)
        {
            if (animator == null || !animator.isActiveAndEnabled) continue;

            // Only trigger controllers that declare the parameter (skips an idle-only
            // controller) so Unity doesn't log a missing-parameter warning.
            if (!HasParameter(animator, _reloadTriggerHash)) continue;
            animator.SetTrigger(_reloadTriggerHash);
        }
    }

    private static bool HasParameter(Animator animator, int paramHash)
    {
        foreach (var p in animator.parameters)
            if (p.nameHash == paramHash) return true;
        return false;
    }
}
