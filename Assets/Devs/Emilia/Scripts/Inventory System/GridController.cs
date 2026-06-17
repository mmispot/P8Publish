using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GridController : MonoBehaviour
{
    public ItemGrid selectedItemGrid;

    [SerializeField] private InventoryItem selectedItem;
    private InventoryItem overlapItem;
    private RectTransform rectTransform;

    [SerializeField] private List<ItemData> items;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private ItemGrid defaultItemGrid;

    public GameObject ItemPrefab => itemPrefab;
    public Transform CanvasTransform => canvasTransform;

    public InventoryHighlight inventoryHighlight;
    public EquipmentSlot hoveredEquipmentSlot;

    InventoryItem itemToHighlight;

    private void Awake()
    {
        inventoryHighlight = GetComponent<InventoryHighlight>();
    }

    private void Update()
    {
        DragItem();
        HandleHighlight();

        if (selectedItemGrid == null) { return; }

        if (Keyboard.current.rKey.wasPressedThisFrame && selectedItem != null)
            RotateItem();

        if (Keyboard.current.cKey.wasPressedThisFrame)
            DeleteHeldItem();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            LMBPress();
    }

    // ─── Grid Toggle ────────────────────────────────────────────────────────────

    public void ToggleGrid()
    {
        if (selectedItemGrid == null) { return; }
        selectedItemGrid.gameObject.SetActive(!selectedItemGrid.gameObject.activeSelf);
    }

    // ─── Item Management ────────────────────────────────────────────────────────

    public void RotateItem()
    {
        if (selectedItem == null) { return; }
        selectedItem.Rotate();
    }

    public void DeleteHeldItem()
    {
        if (selectedItem == null) return;
        Destroy(selectedItem.gameObject);
        selectedItem = null;
    }

    public void InsertItem(InventoryItem itemToInsert)
    {
        if (selectedItemGrid == null)
            selectedItemGrid = defaultItemGrid;

        if (selectedItemGrid == null)
        {
            Debug.LogWarning("InsertItem: No grid available.");
            Destroy(itemToInsert.gameObject);
            return;
        }

        if (itemToInsert.itemData.stackable)
        {
            Debug.Log($"Looking to stack: {itemToInsert.itemData.name}, stackable={itemToInsert.itemData.stackable}, maxStack={itemToInsert.itemData.maxStackSize}");

            InventoryItem existingStack;
            while ((existingStack = selectedItemGrid.FindStackableItem(itemToInsert.itemData)) != null)
            {
                Debug.Log($"Found existing stack: {existingStack.itemData.name}, currentSize={existingStack.currentStackSize}");

                int added = existingStack.AddToStack(itemToInsert.currentStackSize);
                itemToInsert.currentStackSize -= added;

                if (itemToInsert.currentStackSize <= 0)
                {
                    Destroy(itemToInsert.gameObject);
                    return;
                }
            }

            Debug.Log("No existing stack found, placing as new item.");
        }

        Vector2Int posOnGrid = selectedItemGrid.FindSpaceForObject(itemToInsert);

        if (posOnGrid.x == -1)
        {
            Debug.Log("InsertItem: No space in grid.");
            selectedItem = itemToInsert;
            return;
        }

        itemToInsert.UpdateStackText();
        selectedItemGrid.PlaceItem(itemToInsert, posOnGrid.x, posOnGrid.y);
    }

    // ─── Input ──────────────────────────────────────────────────────────────────

    public void LMBPress()
    {
        // Equip held item into hovered equipment slot
        if (selectedItem != null && hoveredEquipmentSlot != null)
        {
            bool equipped = hoveredEquipmentSlot.TryEquipItem(selectedItem);
            if (equipped)
                selectedItem = null;
            else
                Debug.Log("Wrong item type for this slot");
            return;
        }

        // Pick up from hovered equipment slot
        if (selectedItem == null && hoveredEquipmentSlot != null)
        {
            if (hoveredEquipmentSlot.equippedItem != null)
            {
                selectedItem = hoveredEquipmentSlot.UnequipItem();
                rectTransform = selectedItem.GetComponent<RectTransform>();
                rectTransform.SetParent(canvasTransform);
                rectTransform.localScale = Vector3.one;

                CanvasGroup cg = selectedItem.GetComponent<CanvasGroup>();
                if (cg != null) cg.blocksRaycasts = false;
            }
            return;
        }

        if (selectedItemGrid == null) { return; }

        Vector2Int tileGridPosition = GetTileGridPosition();

        if (selectedItem == null)
            PickUpItem(tileGridPosition);
        else
            PlaceItem(tileGridPosition);
    }

    // ─── Grid Interaction ───────────────────────────────────────────────────────

    public Vector2Int GetTileGridPosition()
    {
        return selectedItemGrid.GetTileGridPosition(Mouse.current.position.ReadValue());
    }

    public void PlaceItem(Vector2Int tileGridPosition)
    {
        if (!selectedItemGrid.BoundaryCheck(tileGridPosition.x, tileGridPosition.y, 1, 1))
            return;

        InventoryItem itemUnderCursor = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

        // Stack onto item under cursor if compatible
        if (itemUnderCursor != null
            && itemUnderCursor != selectedItem
            && selectedItem.itemData.stackable
            && itemUnderCursor.itemData == selectedItem.itemData
            && itemUnderCursor.currentStackSize < itemUnderCursor.itemData.maxStackSize)
        {
            int added = itemUnderCursor.AddToStack(selectedItem.currentStackSize);
            int leftover = selectedItem.currentStackSize - added;

            if (leftover <= 0)
            {
                Destroy(selectedItem.gameObject);
                selectedItem = null;
            }
            else
            {
                selectedItem.currentStackSize = leftover;
                selectedItem.UpdateStackText();
            }
            return;
        }

        CanvasGroup canvasGroup = selectedItem.GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        bool complete = selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y, ref overlapItem);
        if (complete)
        {
            selectedItem = null;
            if (overlapItem != null)
            {
                selectedItem = overlapItem;
                overlapItem = null;
                rectTransform = selectedItem.GetComponent<RectTransform>();
            }
        }
    }

    public void PickUpItem(Vector2Int tileGridPosition)
    {
        selectedItem = selectedItemGrid.PickupItem(tileGridPosition.x, tileGridPosition.y);

        if (selectedItem != null)
        {
            rectTransform = selectedItem.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = selectedItem.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }
    }

    // ─── Highlight ──────────────────────────────────────────────────────────────

    private void HandleHighlight()
    {
        if (selectedItemGrid == null) { return; }

        Vector2Int positionOnGrid = GetTileGridPosition();

        if (!selectedItemGrid.BoundaryCheck(positionOnGrid.x, positionOnGrid.y, 1, 1))
        {
            inventoryHighlight.Show(false);
            return;
        }

        if (selectedItem == null)
        {
            itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);

            if (itemToHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetHighlightSize(itemToHighlight);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToHighlight, itemToHighlight.tileGridPosition.x, itemToHighlight.tileGridPosition.y);
            }
            else
            {
                inventoryHighlight.Show(false);
            }
        }
        else
        {
            inventoryHighlight.Show(selectedItemGrid.BoundaryCheck(positionOnGrid.x, positionOnGrid.y, selectedItem.WIDTH, selectedItem.HEIGHT));
            inventoryHighlight.SetHighlightSize(selectedItem);
            inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }

    // ─── Drag ───────────────────────────────────────────────────────────────────

    private void DragItem()
    {
        if (selectedItem != null)
            rectTransform.position = Mouse.current.position.ReadValue();
    }

    // ─── Random Item (debug) ────────────────────────────────────────────────────

    private void InsertRandomItem()
    {
        CreateRandomItem();
        InventoryItem itemToInsert = selectedItem;
        selectedItem = null;
        InsertItem(itemToInsert);
    }

    private void CreateRandomItem()
    {
        InventoryItem inventoryItem = Instantiate(itemPrefab).GetComponent<InventoryItem>();
        selectedItem = inventoryItem;

        rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTransform);

        CanvasGroup canvasGroup = inventoryItem.GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        int selectedItemID = Random.Range(0, items.Count);
        inventoryItem.Set(items[selectedItemID]);
    }
}