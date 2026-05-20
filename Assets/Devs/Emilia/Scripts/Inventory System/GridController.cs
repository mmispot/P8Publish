using UnityEngine;
using System.Collections.Generic;

public class GridController : MonoBehaviour
{
    public ItemGrid selectedItemGrid;

    [SerializeField] private InventoryItem selectedItem;
    private RectTransform rectTransform;

    [SerializeField] private List<ItemData> items;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform canvasTransform;

    private void Update()
    {
        DragItem();
        if (selectedItemGrid == null) { return; }

        //TEMPORARY RANDOM ITEM FUNCTION
        if (Input.GetKeyDown(KeyCode.Q) && selectedItem == null)
        {
            CreateRandomItem();
        }


        if (Input.GetMouseButtonDown(0))
        {
            LMBPress();
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
        Vector2Int tileGridPosition = selectedItemGrid.GetTileGridPosition(Input.mousePosition);

        if (selectedItem == null)
        {
            PickUpItem(tileGridPosition)
        }
        else
        {
            PlaceItem(tileGridPosition);
        }
    }

    private void PlaceItem(Vector2Int tileGridPosition)
    {
        CanvasGroup canvasGroup = selectedItem.GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        bool complete = selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y);
        if (complete)
        {
            selectedItem = null;
        }
    }

    private void PickUpItem(Vector2Int tileGridPosition)
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
            rectTransform.position = Input.mousePosition;
        }
    }
}
