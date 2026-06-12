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

    private void Awake()
    {
        inventoryHighlight = GetComponent<InventoryHighlight>();
    }

    private void Update()
    {
        DragItem();

        HandleHighlight();

        if (selectedItemGrid == null) { return; }
        
        if (Keyboard.current.qKey.wasPressedThisFrame && selectedItem == null) //creates random item and places it, if it cant place it holds in hand
        {
            InsertRandomItem();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame && selectedItem != null) //rotates item in hand
        {
            RotateItem();
        }

        if (Keyboard.current.cKey.wasPressedThisFrame) //delete item in hand, if there is one
        {
            DeleteHeldItem();
        }


        if (Mouse.current.leftButton.wasPressedThisFrame) 
        {
            LMBPress();
        }
    }

    public void ToggleGrid() //needed for chests and stuff, toggles visibility of the grid when opening/closing chest etc
    {
        if (selectedItemGrid == null) { return; }
        selectedItemGrid.gameObject.SetActive(!selectedItemGrid.gameObject.activeSelf);
    }  

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

    private void InsertRandomItem()
    {
        CreateRandomItem(); //change this to param for item to be picked up to auto add to inv
        InventoryItem itemToInsert = selectedItem;
        selectedItem = null;

        InsertItem(itemToInsert);
    }

    public void InsertItem(InventoryItem itemToInsert)
    {
        Vector2Int posOnGrid = selectedItemGrid.FindSpaceForObject(itemToInsert);

        if (posOnGrid.x == -1)
        {
            Debug.Log("No space for item");
            selectedItem = itemToInsert; // player holds it instead
            return;
        }

        selectedItemGrid.PlaceItem(itemToInsert, posOnGrid.x, posOnGrid.y);
    }

    InventoryItem itemToHighlight;

    private void HandleHighlight()
    {
        if (selectedItemGrid == null) { return; }

        Vector2Int positionOnGrid = GetTileGridPosition();

        // Guard against out-of-bounds positions (e.g. mouse over equipment slots)
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

        // using canvasgroup because otherwise canvas isn't recognised (item prevents onpointerenter from working) and raycast blocking is needed to prevent picking up the item while dragging
        CanvasGroup canvasGroup = inventoryItem.GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        int selectedItemID = Random.Range(0, items.Count);
        inventoryItem.Set(items[selectedItemID]);
    }

    public EquipmentSlot hoveredEquipmentSlot;

    public void LMBPress()
    {
        // Priority: try to equip into a hovered slot first
        if (selectedItem != null && hoveredEquipmentSlot != null)
        {
            bool equipped = hoveredEquipmentSlot.TryEquipItem(selectedItem);
            if (equipped)
            {
                selectedItem = null;
                return;
            }
            else
            {
                Debug.Log("Wrong item type for this slot");
                return;
            }
        }

        // Check if we're clicking on an equipment slot to pick up
        if (selectedItem == null && hoveredEquipmentSlot != null)
        {
            if (hoveredEquipmentSlot.equippedItem != null)
            {
                selectedItem = hoveredEquipmentSlot.UnequipItem();
                rectTransform = selectedItem.GetComponent<RectTransform>();

                // Re-parent to canvas so DragItem works correctly
                rectTransform.SetParent(canvasTransform);
                rectTransform.localScale = Vector3.one;

                CanvasGroup cg = selectedItem.GetComponent<CanvasGroup>();
                if (cg != null) cg.blocksRaycasts = false;
            }
            return;
        }

        if (selectedItemGrid == null) { return; }

        Vector2Int tileGridPosition = GetTileGridPosition();
        if (selectedItem == null) PickUpItem(tileGridPosition);
        else PlaceItem(tileGridPosition);
    }

    public Vector2Int GetTileGridPosition()
    {

        Vector2Int tileGridPosition = selectedItemGrid.GetTileGridPosition(Mouse.current.position.ReadValue());
        return tileGridPosition; 
    }

    public void PlaceItem(Vector2Int tileGridPosition)
    {
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

    private void DragItem()
    {
        if (selectedItem != null)
        {
            rectTransform.position = Mouse.current.position.ReadValue();
        }
    }
}
