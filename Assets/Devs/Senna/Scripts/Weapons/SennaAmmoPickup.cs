using UnityEngine;
using UnityEngine.Events;

// World ammo pickup. Calls SennaAmmoSystem.AddReserve() on the player's ammo
// component — that method already handles both inventory-linked and standalone
// reserve modes, so this pickup works in any scene without extra wiring.
// Hooks into SennaPlayerInteractor via ISennaInteractable (F key, same as all pickups).
public class SennaAmmoPickup : MonoBehaviour, ISennaInteractable
{
    [SerializeField] private string displayName = "Ammo";
    [SerializeField] private int amount = 10;

    [Header("Player Reference")]
    [Tooltip("The SennaAmmoSystem on the player. AddReserve() routes to inventory automatically when wired.")]
    [SerializeField] private SennaAmmoSystem ammoSystem;

    public UnityEvent onPickedUp;

    private bool _pickedUp;

    public string PromptText => $"[F] Pick up {displayName} ({amount})";
    public bool CanInteract => !_pickedUp;

    public void Interact(GameObject interactor)
    {
        if (_pickedUp) return;
        _pickedUp = true;

        SennaAmmoSystem target = ammoSystem;
        if (target == null)
            target = interactor.GetComponentInChildren<SennaAmmoSystem>()
                  ?? interactor.GetComponentInParent<SennaAmmoSystem>();

        if (target != null)
            target.AddReserve(amount);
        else
            Debug.LogWarning("SennaAmmoPickup: no SennaAmmoSystem found on interactor.");

        onPickedUp?.Invoke();
        gameObject.SetActive(false);
    }
}
