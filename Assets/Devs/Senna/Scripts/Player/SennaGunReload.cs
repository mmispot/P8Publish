using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Drives the reload: on the Reload action (R) it force-restarts the "Reload" state on
// the arms + gun animators, then refills the magazine once the animation finishes.
// Mirrors SennaGunFeel's approach exactly — both animators get re-kicked on the same
// frame with CrossFadeInFixedTime, no Animator parameters, so the two halves of the
// reload can never drift apart. Firing is blocked while IsReloading is true
// (SchootingRaycast checks it before each shot).
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
    [SerializeField] private string reloadStateName = "Reload";
    // Fallback for a controller whose reload state kept its FBX take name (e.g.
    // "TT33Rig|GunTT33_Reload"). Checked per animator; controllers with neither
    // state are skipped so nothing else gets restarted and pops.
    [SerializeField] private string gunReloadStateName = "Reload";

    [Tooltip("Seconds before the magazine refills. Match this to the reload clip length.")]
    [SerializeField] private float reloadDuration = 1.5f;

    public bool IsReloading { get; private set; }

    private int _reloadStateHash;
    private int _gunReloadStateHash;
    private Coroutine _reloadRoutine;

    private void Awake()
    {
        _reloadStateHash = Animator.StringToHash(reloadStateName);
        _gunReloadStateHash = Animator.StringToHash(gunReloadStateName);

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

            // Pick whichever reload state this animator's controller actually has;
            // no match (e.g. an idle-only controller) -> skip so its state never pops.
            int stateHash =
                animator.HasState(0, _reloadStateHash)    ? _reloadStateHash    :
                animator.HasState(0, _gunReloadStateHash) ? _gunReloadStateHash : 0;

            if (stateHash == 0) continue;

            // time 0 force-restarts the state even mid-play, so spamming R re-kicks the
            // reload instead of stacking like SetTrigger would.
            animator.CrossFadeInFixedTime(stateHash, 0f, 0, 0f);
        }
    }
}
