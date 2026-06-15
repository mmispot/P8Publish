using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItem : MonoBehaviour
{
    public ItemData itemData;
    public Vector2Int tileGridPosition;

    public int currentStackSize = 1; // starts at 1 since the item itself counts as 1 in the stack

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

    public void Update()
    {
        UpdateStackText();
    }

    [SerializeField] private TextMeshProUGUI stackCountText;

    private void Start()
    {
        stackCountText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void UpdateStackText()
    {
        if (stackCountText == null) return;

        // Only show a number if stackable and more than 1
        stackCountText.text = (itemData.stackable && currentStackSize > 1)
            ? currentStackSize.ToString()
            : "";
    }

    // Returns how much was actually added (in case of overflow)
    public int AddToStack(int amount)
    {
        int spaceLeft = itemData.maxStackSize - currentStackSize;
        int amountToAdd = Mathf.Min(spaceLeft, amount);
        currentStackSize += amountToAdd;
        UpdateStackText();
        return amountToAdd; // leftover = amount - amountToAdd
    }

    public void Set(ItemData itemData)
    {
        this.itemData = itemData;

        GetComponent<Image>().sprite = itemData.itemIcon;

        Vector2 size = new Vector2();
        size.x = itemData.width * ItemGrid.tileSizeWidth;
        size.y = itemData.height * ItemGrid.tileSizeHeight;
        GetComponent<RectTransform>().sizeDelta = size;

        currentStackSize = 1;
        UpdateStackText();
    }

    public void Rotate()
    {
        rotated = !rotated;

        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.rotation = Quaternion.Euler(0, 0, rotated == true ? 90f : 0f);

    }
}