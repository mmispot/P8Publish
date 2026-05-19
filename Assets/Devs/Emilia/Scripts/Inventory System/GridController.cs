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

        int selectedItemID = Random.Range(0, items.Count);
        inventoryItem.Set(items[selectedItemID]);
    }

    private void DragItem()
    {
        if (selectedItem != null)
        {
            rectTransform.position = Input.mousePosition;
        }
    }

    public void LMBPress()
    {
        Vector2Int tileGridPosition = selectedItemGrid.GetTileGridPosition(Input.mousePosition);

        if (selectedItem == null)
        {
            selectedItem = selectedItemGrid.PickupItem(tileGridPosition.x, tileGridPosition.y);

            if (selectedItem != null)
            {
                rectTransform = selectedItem.GetComponent<RectTransform>();
            }
        }
        else
        {
            selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y);
            selectedItem = null;
        }
    }
}
