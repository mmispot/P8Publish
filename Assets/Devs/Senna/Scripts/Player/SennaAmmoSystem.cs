using UnityEngine;
using UnityEngine.Events;

// Owns the gun's ammo state: mag + reserve.
// Reserve can be backed by the inventory (quest scene) or a standalone counter (other scenes).
// Wire inventoryGrid + ammoItemData in the inspector to use inventory; leave them empty to use
// the local reserveAmmo field as before — other scenes are unaffected.
public class SennaAmmoSystem : MonoBehaviour
{
    [Header("Magazine")]
    [SerializeField] private int magazineSize = 12;
    [SerializeField] private int currentInMag = 12;

    [Header("Reserve (standalone fallback)")]
    [SerializeField] private int reserveAmmo = 60;

    [Header("Inventory Link (quest scene)")]
    [SerializeField] private ItemGrid inventoryGrid;
    [SerializeField] private ItemData ammoItemData;
    [SerializeField] private GridController gridController;

    [Header("Events")]
    public UnityEvent<int, int> onAmmoChanged;   // passes (currentInMag, ReserveAmmo)

    public int CurrentInMag => currentInMag;
    public int ReserveAmmo => (inventoryGrid != null && ammoItemData != null)
        ? inventoryGrid.CountAmmoOfType(ammoItemData)
        : reserveAmmo;
    public int MagazineSize => magazineSize;
    public bool HasAmmo => currentInMag > 0;
    public bool IsFull => currentInMag >= magazineSize;

    private void Awake()
    {
        currentInMag = Mathf.Clamp(currentInMag, 0, magazineSize);
        onAmmoChanged.Invoke(currentInMag, ReserveAmmo);
    }

    public bool TryConsume()
    {
        if (currentInMag <= 0) return false;
        currentInMag--;
        onAmmoChanged.Invoke(currentInMag, ReserveAmmo);
        return true;
    }

    public void Reload()
    {
        int reserve = ReserveAmmo;
        if (IsFull || reserve <= 0) return;
        int needed = magazineSize - currentInMag;
        int loaded = Mathf.Min(needed, reserve);
        currentInMag += loaded;
        if (inventoryGrid != null && ammoItemData != null)
            inventoryGrid.ConsumeAmmoOfType(ammoItemData, loaded);
        else
            reserveAmmo -= loaded;
        onAmmoChanged.Invoke(currentInMag, ReserveAmmo);
    }

    // Adds ammo to the inventory when linked, otherwise the local reserve.
    public void AddReserve(int amount)
    {
        if (amount <= 0) return;
        if (inventoryGrid != null && ammoItemData != null && gridController != null)
        {
            inventoryGrid.EnsureInitialized();
            AddAmmoToInventory(amount);
        }
        else
        {
            reserveAmmo += amount;
            onAmmoChanged.Invoke(currentInMag, ReserveAmmo);
        }
    }

    private void AddAmmoToInventory(int amount)
    {
        int toAdd = Mathf.Clamp(amount, 1, ammoItemData.maxStackSize);
        var go = Object.Instantiate(gridController.ItemPrefab, gridController.CanvasTransform);
        var item = go.GetComponent<InventoryItem>();
        item.Set(ammoItemData);
        item.currentStackSize = toAdd;
        item.UpdateStackText();

        // blocksRaycasts must stay true for placed items so the player can drag them.
        // (Only set to false when an item is being held/dragged by the cursor.)
        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = true;

        gridController.InsertItem(item, inventoryGrid);
    }
}
