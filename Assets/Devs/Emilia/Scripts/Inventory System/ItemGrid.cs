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

        //InventoryItem inventoryItem = Instantiate(inventoryItemPrefab, transform).GetComponent<InventoryItem>();
        //PlaceItem(inventoryItem, 1, 1);

        //inventoryItem = Instantiate(inventoryItemPrefab, transform).GetComponent<InventoryItem>();
        //PlaceItem(inventoryItem, 5, 2);

        //inventoryItem = Instantiate(inventoryItemPrefab, transform).GetComponent<InventoryItem>();
        //PlaceItem(inventoryItem, 8, 2);
    }

    public InventoryItem PickupItem(int x, int y) //haalt item van een grid en geeft die terug aan de itemcontroller
    {
        InventoryItem toReturn = inventoryItemSlot[x, y];
        inventoryItemSlot[x, y] = null;
        return toReturn;
    }

    private void Init(int width, int height) //maakt een 2D array aan voor de items en zet de grootte van het grid
    {
        inventoryItemSlot = new InventoryItem[width, height];
        Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
        rectTransform.sizeDelta = size;
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

    public void PlaceItem(InventoryItem inventoryItem, int posX, int posY) //plaatst een item op de grid en zet de positie in een 2d array om bij te houden wat occupied is en wat niet
    {
        RectTransform itemRectTransform = inventoryItem.GetComponent<RectTransform>();
        itemRectTransform.SetParent(rectTransform);
        
        for (int x = 0; x < inventoryItem.itemData.width; x++)
        {
            for (int y = 0; y < inventoryItem.itemData.height; y++)
            {
                inventoryItemSlot[posX + x, posY + y] = inventoryItem;
            }
        }

        Vector2 position = new Vector2();
        position.x = posX * tileSizeWidth + tileSizeWidth * inventoryItem.itemData.width / 2;
        position.y = -(posY * tileSizeHeight + tileSizeHeight * inventoryItem.itemData.height / 2);

        itemRectTransform.localPosition = position;
    }
}