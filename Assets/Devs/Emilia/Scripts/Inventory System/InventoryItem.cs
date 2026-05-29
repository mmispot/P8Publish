using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
    public ItemData itemData;
    public Vector2Int tileGridPosition;

    public bool rotated = false;

    public int HEIGHT //changes width and height around when rotated
    {
        get
        {
            if (rotated == false)
            {
                return itemData.height;
            }
            return itemData.width;
        }
    }

    public int WIDTH //changes width and height around when rotated
    {
        get
        {
            if (rotated == false)
            {
                return itemData.width;
            }
            return itemData.height;
        }
    }

    public void Set(ItemData itemData)
    {
        this.itemData = itemData;

        GetComponent<Image>().sprite = itemData.itemIcon;

        Vector2 size = new Vector2();
        size.x = itemData.width * ItemGrid.tileSizeWidth;
        size.y = itemData.height * ItemGrid.tileSizeHeight;
        GetComponent<RectTransform>().sizeDelta = size;
    }

    public void Rotate()
    {
        rotated = !rotated;

        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.rotation = Quaternion.Euler(0, 0, rotated == true ? 90f : 0f);

    }
}