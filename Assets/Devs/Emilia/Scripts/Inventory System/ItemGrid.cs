using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGrid : MonoBehaviour
{
    // Tutorial used: https://www.youtube.com/watch?v=2ajD1GDbEzA&t=452s

    //Size in px of the individual inventory tiles! Artists beware
    public const float tileSizeWidth = 64;
    public const float tileSizeHeight = 64;

    InventoryItem[,] inventoryItemSlot;

    RectTransform rectTransform;

    [SerializeField] int gridSizeWidth = 20;
    [SerializeField] int gridSizeHeight = 10;

    [SerializeField] GameObject inventoryItemPrefab;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        Init(gridSizeWidth, gridSizeHeight);
    }

    public InventoryItem FindStackableItem(ItemData itemData)
    {
        if (!itemData.stackable) return null;

        for (int y = 0; y < gridSizeHeight; y++)
        {
            for (int x = 0; x < gridSizeWidth; x++)
            {
                InventoryItem item = inventoryItemSlot[x, y];

                if (item != null
                    && item.itemData == itemData
                    && item.currentStackSize < itemData.maxStackSize)
                {
                    return item;
                }
            }
        }

        return null;
    }

    public InventoryItem PickupItem(int x, int y) //haalt item van een grid en geeft die terug aan de itemcontroller
    {
        InventoryItem toReturn = inventoryItemSlot[x, y];

        if (toReturn == null) { return null; }

        CleanGridReference(toReturn);
        
        return toReturn;
    }

    private void CleanGridReference(InventoryItem item) //pakt reference van een item en maakt alle plekken op het grid waar dat item stond weer leeg
    {
        for (int ix = 0; ix < item.WIDTH; ix++)
        {
            for (int iy = 0; iy < item.HEIGHT; iy++)
            {
                inventoryItemSlot[item.tileGridPosition.x + ix, item.tileGridPosition.y + iy] = null;
            }
        }
    }

    private void Init(int width, int height) //maakt een 2D array aan voor de items en zet de grootte van het grid
    {
        inventoryItemSlot = new InventoryItem[width, height];
        Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
        rectTransform.sizeDelta = size;
    }
    
    internal InventoryItem GetItem(int x, int y)
    {
        return inventoryItemSlot[x, y];
    }

    Vector2 positionOnGrid = new Vector2();
    Vector2Int tileGridPosition = new Vector2Int();

    public Vector2Int GetTileGridPosition(Vector2 mousePosition) //zet de positie van de muis om naar een positie op het grid
    {
        positionOnGrid.x = mousePosition.x - rectTransform.position.x;
        positionOnGrid.y = rectTransform.position.y - mousePosition.y;

        tileGridPosition.x = (int)(positionOnGrid.x / tileSizeWidth);
        tileGridPosition.y = (int)(positionOnGrid.y / tileSizeHeight);

        return tileGridPosition;
    }

    public Vector2Int FindSpaceForObject(InventoryItem itemToInsert)
    {
        int height = gridSizeHeight - itemToInsert.HEIGHT +1;
        int width = gridSizeWidth - itemToInsert.WIDTH +1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (CheckAvailableSpace(x, y, itemToInsert.WIDTH, itemToInsert.HEIGHT))
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1); // sentinel for "not found"
    }

    public bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem) //places item MAAR checkt eerst
    {
        overlapItem = null;

        if (BoundaryCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT) == false) //checks of item in de grid past
        {
            Debug.Log("Item doesn't fit in the grid");
            return false;
        }

        if (CheckAvailableSpace(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT) == false) //checkt of item overlapt met een ander item
        {
            Debug.Log("Item overlaps with another item");
            overlapItem = null;
            return false;
        }

        if (overlapItem != null)
        {
            CleanGridReference(overlapItem);
        }

        PlaceItem(inventoryItem, posX, posY);

        return true;
    }

    public void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        RectTransform itemRectTransform = inventoryItem.GetComponent<RectTransform>();
        itemRectTransform.SetParent(rectTransform);

        inventoryItem.tileGridPosition = new Vector2Int(posX, posY);

        for (int x = 0; x < inventoryItem.WIDTH; x++)
        {
            for (int y = 0; y < inventoryItem.HEIGHT; y++)
            {
                inventoryItemSlot[posX + x, posY + y] = inventoryItem; //actual plaatsen van het item in de grid array
            }
        }

        //inventoryItem.positionOnGrid.x = posX;
        //inventoryItem.positionOnGrid.y = posY;
        Vector2 position = CalculatePositionOnGrid(inventoryItem, posX, posY);

        itemRectTransform.localPosition = position;
    }

    public Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        Vector2 position = new Vector2();
        position.x = posX * tileSizeWidth + tileSizeWidth * inventoryItem.WIDTH / 2;
        position.y = -(posY * tileSizeHeight + tileSizeHeight * inventoryItem.HEIGHT / 2);
        return position;
    }

    private bool CheckAvailableSpace(int posX, int posY, int width, int height) //checkt voor overlap met ander item
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (inventoryItemSlot[posX + x, posY + y] != null)
                {
                    return false; 
                }
            }
        }

        return true;
    }

    public int CountAmmoOfType(ItemData ammoData)
    {
        if (ammoData == null || inventoryItemSlot == null) return 0;
        int total = 0;
        for (int y = 0; y < gridSizeHeight; y++)
            for (int x = 0; x < gridSizeWidth; x++)
            {
                var item = inventoryItemSlot[x, y];
                if (item != null && item.itemData == ammoData
                    && item.tileGridPosition.x == x && item.tileGridPosition.y == y)
                    total += item.currentStackSize;
            }
        return total;
    }

    // Returns how many rounds were actually consumed (may be less than amount if inventory is short).
    public int ConsumeAmmoOfType(ItemData ammoData, int amount)
    {
        if (ammoData == null || amount <= 0) return 0;
        int remaining = amount;
        for (int y = 0; y < gridSizeHeight && remaining > 0; y++)
            for (int x = 0; x < gridSizeWidth && remaining > 0; x++)
            {
                var item = inventoryItemSlot[x, y];
                if (item == null || item.itemData != ammoData
                    || item.tileGridPosition.x != x || item.tileGridPosition.y != y)
                    continue;
                int take = Mathf.Min(remaining, item.currentStackSize);
                item.currentStackSize -= take;
                remaining -= take;
                if (item.currentStackSize <= 0)
                {
                    CleanGridReference(item);
                    Destroy(item.gameObject);
                }
            }
        return amount - remaining;
    }

    private bool PositionCheck(int posX, int posY) //checkt of een positie binnen de grenzen van het grid ligt (MATH)
    {
        if (posX < 0 || posY < 0)
        {
            return false;
        }

        if (posX >= gridSizeWidth || posY >= gridSizeHeight)
        {
            return false;
        }

        return true;
    }

    public bool BoundaryCheck(int posX, int posY, int width, int height) //checkt of een positie binnen de boundaries
    {
        if (PositionCheck(posX, posY) == false) { return false; }

        posX += width-1;
        posY += height-1;

        if (PositionCheck(posX, posY) == false) { return false; }

        return true;
    }
}