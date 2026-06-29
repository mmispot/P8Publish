using UnityEngine;
using UnityEngine.Events;

// World pickup for a gun. Place on a GameObject with a trigger collider.
// Assign playerGun to the disabled gun GameObject on the player rig — pressing
// F enables it and hides this world prop. Hooks into SennaPlayerInteractor via
// ISennaInteractable, same as all other pickups in this project.
// Optionally assign inventoryItemData (e.g. Secondary.asset) to also snap the
// gun icon into the matching EquipmentSlot automatically on pickup.
public class SennaGunPickup : MonoBehaviour, ISennaInteractable
{
    [SerializeField] private SennaGunData gunData;

    [Header("Player Gun")]
    [Tooltip("The disabled gun GameObject on the player rig that gets enabled on pickup.")]
    [SerializeField] private GameObject playerGun;

    [Header("Inventory")]
    [Tooltip("Assign the ItemData asset (e.g. Secondary.asset) to auto-equip this gun to the matching equipment slot on pickup. Leave empty to skip inventory.")]
    [SerializeField] private ItemData inventoryItemData;

    [Header("Quest")]
    [Tooltip("If set, reports this key to SennaQuestManager when the gun is picked up.")]
    [SerializeField] private string questInteractKey;

    public UnityEvent onPickedUp;

    private bool _pickedUp;

    public string PromptText => gunData != null ? $"[F] Pick up {gunData.displayName}" : "[F] Pick up Gun";
    public bool CanInteract => !_pickedUp;

    public void Interact(GameObject interactor)
    {
        if (_pickedUp) return;
        _pickedUp = true;

        if (playerGun != null)
            playerGun.SetActive(true);
        else
            Debug.LogWarning("SennaGunPickup: playerGun not assigned.");

        if (inventoryItemData != null)
            AutoEquipToInventory();

        if (!string.IsNullOrEmpty(questInteractKey))
            SennaQuestManager.Instance?.ReportInteractionCompleted(questInteractKey);

        onPickedUp?.Invoke();
        gameObject.SetActive(false);
    }

    private void AutoEquipToInventory()
    {
        var gc = Object.FindFirstObjectByType<GridController>(FindObjectsInactive.Include);
        if (gc == null)
        {
            Debug.LogWarning("SennaGunPickup: No GridController found — gun not added to inventory.");
            return;
        }

        var ig = Object.FindFirstObjectByType<ItemGrid>(FindObjectsInactive.Include);
        if (ig != null) ig.EnsureInitialized();

        var go = Object.Instantiate(gc.ItemPrefab, gc.CanvasTransform);
        var invItem = go.GetComponent<InventoryItem>();
        invItem.Set(inventoryItemData);
        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = true;

        // Find the equipment slot that matches this gun's item type (Primary/Secondary).
        EquipmentSlot targetSlot = null;
        foreach (var slot in Object.FindObjectsByType<EquipmentSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (slot.acceptedType == inventoryItemData.itemType)
            {
                targetSlot = slot;
                break;
            }
        }

        if (targetSlot != null)
            targetSlot.TryEquipItem(invItem, ig);
        else
            gc.InsertItem(invItem, ig); // no matching slot found — place in regular grid
    }
}
