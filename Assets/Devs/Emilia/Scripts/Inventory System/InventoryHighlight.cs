using UnityEngine;

public class InventoryHighlight : MonoBehaviour
{
    //this script takes care of the highlight of inv slots (makes it more visible when using pngs)
    [SerializeField] RectTransform highlighter;

    public void SetHighlightSize(InventoryItem targetItem)
    {
        Vector2 size = new Vector2();
        size.x = targetItem.itemData.width * ItemGrid.tileSizeWidth;
        size.y = targetItem.itemData.height * ItemGrid.tileSizeHeight;
        highlighter.sizeDelta = size;
    }

    public void SetPosition(ItemGrid targetGrid, InventoryItem targetItem, Vector2Int gridPosition)
    {
        highlighter.SetParent(targetGrid.GetComponent<RectTransform>());

        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, gridPosition.x, gridPosition.y);

        highlighter.localPosition = pos;
    }
}
