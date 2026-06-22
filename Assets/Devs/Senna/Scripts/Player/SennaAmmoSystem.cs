using UnityEngine;
using UnityEngine.Events;

// Owns the gun's ammo state: rounds loaded in the magazine plus a reserve pool.
// SchootingRaycast calls TryConsume() before firing, so an empty mag blocks the shot.
// v1 is count-only: Reload()/AddReserve() exist for the reload + inventory work that lands later.
public class SennaAmmoSystem : MonoBehaviour
{
    [Header("Magazine")]
    [SerializeField] private int magazineSize = 12;
    [SerializeField] private int currentInMag = 12;

    [Header("Reserve")]
    [SerializeField] private int reserveAmmo = 60;

    [Header("Events")]
    public UnityEvent<int, int> onAmmoChanged;   // passes (currentInMag, reserveAmmo)

    public int CurrentInMag => currentInMag;
    public int ReserveAmmo => reserveAmmo;
    public int MagazineSize => magazineSize;
    public bool HasAmmo => currentInMag > 0;
    public bool IsFull => currentInMag >= magazineSize;

    private void Awake()
    {
        currentInMag = Mathf.Clamp(currentInMag, 0, magazineSize);
        onAmmoChanged.Invoke(currentInMag, reserveAmmo);
    }

    // Called by SchootingRaycast before it fires. Returns false when the mag is empty.
    public bool TryConsume()
    {
        if (currentInMag <= 0) return false;

        currentInMag--;
        onAmmoChanged.Invoke(currentInMag, reserveAmmo);
        return true;
    }

    // Refills the magazine from the reserve. Not bound to any input yet — reload wiring lands later;
    // this is the method that input/animation will call when it does.
    public void Reload()
    {
        if (IsFull || reserveAmmo <= 0) return;

        int needed = magazineSize - currentInMag;
        int loaded = Mathf.Min(needed, reserveAmmo);

        currentInMag += loaded;
        reserveAmmo -= loaded;
        onAmmoChanged.Invoke(currentInMag, reserveAmmo);
    }

    // Adds rounds to the reserve. Seam for the inventory system to feed picked-up ammo in later.
    public void AddReserve(int amount)
    {
        if (amount <= 0) return;

        reserveAmmo += amount;
        onAmmoChanged.Invoke(currentInMag, reserveAmmo);
    }

    // --- Inventory integration (pending) -------------------------------------------------
    // Ammo will eventually come from the inventory system (Assets/Devs/Emilia/Scripts/Inventory
    // System). Plan: add an ItemType.Ammo to ItemData, then on ammo pickup call AddReserve(stack),
    // and on reload decrement the matching ItemData stack instead of the local reserveAmmo field.
    // Kept standalone for now so this system works without touching Emilia's inventory yet.
    // -------------------------------------------------------------------------------------
}
