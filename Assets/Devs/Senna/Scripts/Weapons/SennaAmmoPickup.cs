using UnityEngine;
using UnityEngine.Events;

// World ammo pickup.
// When Inventory Item Data is assigned (e.g. Ammo.asset): directly inserts the item
// tile into the inventory grid — same pattern as SennaGunPickup. Amount = how many
// mags to add (currentStackSize on the inserted item).
// When Inventory Item Data is left empty: falls back to SennaAmmoSystem.AddReserve()
// so it still works in scenes without an inventory grid.
public class SennaAmmoPickup : MonoBehaviour, ISennaInteractable
{
    [SerializeField] private string displayName = "Ammo";
    [SerializeField] private int amount = 1;

    [Header("Inventory")]
    [Tooltip("Assign Ammo.asset here. The pickup will insert this item directly into the inventory grid, exactly like GunPickup does.")]
    [SerializeField] private ItemData inventoryItemData;

    [Header("Player Reference (fallback when Inventory Item Data is empty)")]
    [Tooltip("Only used when Inventory Item Data is not assigned.")]
    [SerializeField] private SennaAmmoSystem ammoSystem;

    public UnityEvent onPickedUp;

    private bool _pickedUp;

    public string PromptText => $"[F] Pick up {displayName} ({amount})";
    public bool CanInteract => !_pickedUp;

    public void Interact(GameObject interactor)
    {
        if (_pickedUp) return;
        _pickedUp = true;

        if (inventoryItemData != null)
            AddToInventoryGrid();
        else
            AddToAmmoSystem(interactor);

        onPickedUp?.Invoke();
        gameObject.SetActive(false);
    }

    private void AddToInventoryGrid()
    {
        var gc = Object.FindFirstObjectByType<GridController>(FindObjectsInactive.Include);
        if (gc == null)
        {
            Debug.LogWarning("SennaAmmoPickup: No GridController found — ammo not added to inventory.");
            return;
        }

        var ig = Object.FindFirstObjectByType<ItemGrid>(FindObjectsInactive.Include);
        if (ig != null) ig.EnsureInitialized();

        var go = Object.Instantiate(gc.ItemPrefab, gc.CanvasTransform);
        var invItem = go.GetComponent<InventoryItem>();
        invItem.Set(inventoryItemData);
        invItem.currentStackSize = Mathf.Clamp(amount, 1, inventoryItemData.stackable ? inventoryItemData.maxStackSize : 1);
        invItem.UpdateStackText();

        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = true;

        gc.InsertItem(invItem, ig);
    }

    private void AddToAmmoSystem(GameObject interactor)
    {
        SennaAmmoSystem target = ammoSystem;
        if (target == null)
            target = interactor.GetComponentInChildren<SennaAmmoSystem>()
                  ?? interactor.GetComponentInParent<SennaAmmoSystem>();

        if (target != null)
            target.AddReserve(amount);
        else
            Debug.LogWarning("SennaAmmoPickup: no SennaAmmoSystem found on interactor.");
    }
}
