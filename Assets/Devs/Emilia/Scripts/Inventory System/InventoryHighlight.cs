using UnityEngine;
using UnityEngine.UI;

public class InventoryHighlight : MonoBehaviour
{
    //this script takes care of the highlight of inv slots (makes it more visible when using pngs)
    public RectTransform highlighter;

    public void Show(bool visible)
    {
        highlighter.gameObject.SetActive(visible);
    }

    public void SetHighlightSize(InventoryItem targetItem)
    {
        Vector2 size = new Vector2();
        size.x = targetItem.WIDTH * ItemGrid.tileSizeWidth;
        size.y = targetItem.HEIGHT * ItemGrid.tileSizeHeight;
        highlighter.sizeDelta = size;
    }

    public void SetPosition(ItemGrid targetGrid, InventoryItem targetItem, int posX, int posY)
    {
        highlighter.SetParent(targetGrid.GetComponent<RectTransform>()); // RectTransform, not Transform
        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, posX, posY);
        highlighter.localPosition = pos;
    }
}
