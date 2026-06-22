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

    public InventoryHighlight inventoryHighlight;
    public EquipmentSlot hoveredEquipmentSlot;

    public GameObject ItemPrefab => itemPrefab;
    public Transform CanvasTransform => canvasTransform;

    private void Awake()
    {
        inventoryHighlight = GetComponent<InventoryHighlight>();
    }

    private void Update()
    {
        DragItem();
        HandleHighlight();

        // Keyboard shortcuts only make sense when a grid is active and no equipment slot is hovered
        if (selectedItemGrid != null && hoveredEquipmentSlot == null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame && selectedItem == null)
            {
                InsertRandomItem();
            }

            if (Keyboard.current.rKey.wasPressedThisFrame && selectedItem != null)
            {
                RotateItem();
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                DeleteHeldItem();
            }
        }

        // LMB is always checked — equipment slots are valid targets even without a selectedItemGrid
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            LMBPress();
        }
    }

    public void RotateItem()
    {
        if (selectedItem == null) return;
        selectedItem.Rotate();
    }

    public void DeleteHeldItem()
    {
        if (selectedItem == null) return;
        Destroy(selectedItem.gameObject);
        selectedItem = null;
    }

    private void InsertRandomItem()
    {
        CreateRandomItem();
        InventoryItem itemToInsert = selectedItem;
        selectedItem = null;
        InsertItem(itemToInsert);
    }

    // FIX: InsertItem now takes an optional target grid parameter.
    // When swapping an item out of an equipment slot, the equipped item
    // needs to go back into the inventory grid — but selectedItemGrid
    // may be null at that moment (player is hovering the equipment slot,
    // not a grid). Passing the grid explicitly avoids the null-ref.
    public void InsertItem(InventoryItem itemToInsert, ItemGrid targetGrid = null)
    {
        ItemGrid grid = targetGrid ?? selectedItemGrid;

        if (grid == null)
        {
            Debug.LogWarning("InsertItem: no target grid available. Item held in hand.");
            selectedItem = itemToInsert;
            rectTransform = selectedItem.GetComponent<RectTransform>();
            CanvasGroup cg = selectedItem.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = false;
            return;
        }

        if (itemToInsert.itemData.stackable)
        {
            InventoryItem existingStack = grid.FindStackableItem(itemToInsert.itemData);
            if (existingStack != null)
            {
                int added = existingStack.AddToStack(itemToInsert.currentStackSize);
                int leftover = itemToInsert.currentStackSize - added;

                if (leftover <= 0)
                {
                    Destroy(itemToInsert.gameObject);
                    return;
                }
                else
                {
                    itemToInsert.currentStackSize = leftover;
                    itemToInsert.UpdateStackText();
                }
            }
        }

        Vector2Int posOnGrid = grid.FindSpaceForObject(itemToInsert);

        if (posOnGrid.x == -1)
        {
            Debug.Log("No space for item — held in hand.");
            selectedItem = itemToInsert;
            rectTransform = selectedItem.GetComponent<RectTransform>();
            CanvasGroup cg = selectedItem.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = false;
            return;
        }

        grid.PlaceItem(itemToInsert, posOnGrid.x, posOnGrid.y);
    }

    InventoryItem itemToHighlight;

    private void HandleHighlight()
    {
        // When hovering an equipment slot, show no grid highlight
        if (hoveredEquipmentSlot != null)
        {
            inventoryHighlight.Show(false);
            return;
        }

        if (selectedItemGrid == null)
        {
            inventoryHighlight.Show(false);
            return;
        }

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

    private void CreateRandomItem()
    {
        InventoryItem inventoryItem = Instantiate(itemPrefab).GetComponent<InventoryItem>();
        selectedItem = inventoryItem;

        rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTransform);
        rectTransform.SetAsLastSibling();

        CanvasGroup canvasGroup = inventoryItem.GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        int selectedItemID = Random.Range(0, items.Count);
        inventoryItem.Set(items[selectedItemID]);
    }

    public void LMBPress()
    {
        //Case 1: holding an item + hovering an equipment slot = try to equip
        if (selectedItem != null && hoveredEquipmentSlot != null)
        {
            bool equipped = hoveredEquipmentSlot.TryEquipItem(selectedItem, selectedItemGrid);
            if (equipped)
            {
                selectedItem = null;
            }
            else
            {
                Debug.Log("Wrong item type for this slot.");
            }
            return;
        }

        //Case 2: empty hand + hovering an equipment slot = pick up equipped item
        if (selectedItem == null && hoveredEquipmentSlot != null)
        {
            if (hoveredEquipmentSlot.equippedItem != null)
            {
                selectedItem = hoveredEquipmentSlot.UnequipItem();
                rectTransform = selectedItem.GetComponent<RectTransform>();
                rectTransform.SetParent(canvasTransform);
                rectTransform.SetAsLastSibling(); // NEW
                rectTransform.localScale = Vector3.one;

                CanvasGroup cg = selectedItem.GetComponent<CanvasGroup>();
                if (cg != null) cg.blocksRaycasts = false;
            }
            return;
        }

        //Case 3: interacting with the inventory grid
        if (selectedItemGrid == null) return;

        Vector2Int tileGridPosition = GetTileGridPosition();
        if (selectedItem == null) PickUpItem(tileGridPosition);
        else PlaceItem(tileGridPosition);
    }

    public Vector2Int GetTileGridPosition()
    {
        return selectedItemGrid.GetTileGridPosition(Mouse.current.position.ReadValue());
    }

    public void PlaceItem(Vector2Int tileGridPosition)
    {
        if (!selectedItemGrid.BoundaryCheck(tileGridPosition.x, tileGridPosition.y, 1, 1))
            return;

        InventoryItem itemUnderCursor = selectedItemGrid.GetItem(tileGridPosition.x, tileGridPosition.y);

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
            rectTransform.SetParent(canvasTransform); 
            rectTransform.SetAsLastSibling();        

            CanvasGroup canvasGroup = selectedItem.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }
    }

    private void DragItem()
    {
        if (selectedItem != null)
        {
            rectTransform.position = Mouse.current.position.ReadValue();
        }
    }
}