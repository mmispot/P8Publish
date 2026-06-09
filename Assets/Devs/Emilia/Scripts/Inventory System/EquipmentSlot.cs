using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public InventoryItem equippedItem;

    private GridController gridController;

    private void Awake()
    {
        gridController = FindObjectOfType<GridController>();
    }

    [SerializeField] public ItemData.ItemType acceptedType;

    public bool CanAcceptItem(InventoryItem item)
    {
        return item != null && item.itemData.itemType == acceptedType;
    }

    [SerializeField] public bool allowSwap = true; // disable this on crafting slots in Inspector

    public bool TryEquipItem(InventoryItem item)
    {
        if (!CanAcceptItem(item)) return false;

        if (equippedItem != null)
        {
            if (!allowSwap) return false; // ← reject if slot is full and swapping is off

            gridController.InsertItem(equippedItem);
        }

        equippedItem = item;
        SnapItemToSlot(item);
        return true;
    }

    public InventoryItem UnequipItem()
    {
        InventoryItem item = equippedItem;
        equippedItem = null;
        return item;
    }

    private void SnapItemToSlot(InventoryItem item)
    {
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.SetParent(this.transform);

        // Force item to fill the slot exactly
        itemRect.anchorMin = Vector2.zero;
        itemRect.anchorMax = Vector2.one;
        itemRect.offsetMin = Vector2.zero;
        itemRect.offsetMax = Vector2.zero;
        itemRect.localScale = Vector3.one;

        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = true;
    }

    // Tell the GridController we're hovering over this slot
    public void OnPointerEnter(PointerEventData eventData)
    {
        gridController.hoveredEquipmentSlot = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gridController.hoveredEquipmentSlot = null;
    }
}