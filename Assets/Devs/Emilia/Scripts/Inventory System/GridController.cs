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

        //TEMPORARY RANDOM ITEM FUNCTION
        if (Keyboard.current.qKey.wasPressedThisFrame && selectedItem == null)
        {
            CreateRandomItem();
        }


        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            LMBPress();
        }
    }

    InventoryItem itemToHighlight;

    private void HandleHighlight()
    {
        Vector2Int positionOnGrid = GetTileGridPosition();

        if (selectedItem == null)
        {
            itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);

            if (itemToHighlight != null)
            {
                inventoryHighlight.SetHighlightSize(itemToHighlight);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToHighlight, itemToHighlight.tileGridPosition);
            }
        }
        else
        {
            inventoryHighlight.SetHighlightSize(selectedItem);
            inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positionOnGrid);
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

    public void LMBPress()
    {
        Vector2Int tileGridPosition = GetTileGridPosition();

        if (selectedItem == null)
        {
            PickUpItem(tileGridPosition);
        }
        else
        {
            PlaceItem(tileGridPosition);
        }
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
