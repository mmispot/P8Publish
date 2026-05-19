using UnityEngine;

public class GridController : MonoBehaviour
{
    public ItemGrid selectedItemGrid;

    [SerializeField] private InventoryItem selectedItem;
    private RectTransform rectTransform;

    private void Update()
    {
        if (selectedItem != null)
        {
            rectTransform.position = Input.mousePosition;
        }

        if (selectedItemGrid == null) { return; }

        if (Input.GetMouseButtonDown(0))
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
}
