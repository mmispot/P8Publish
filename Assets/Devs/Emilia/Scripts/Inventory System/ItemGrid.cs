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
    Canvas parentCanvas;

    [SerializeField] int gridSizeWidth = 20;
    [SerializeField] int gridSizeHeight = 10;

    [SerializeField] GameObject inventoryItemPrefab;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
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

        if (toReturn == null) { return null; }

        CleanGridReference(toReturn);
        
        return toReturn;
    }

    private void CleanGridReference(InventoryItem item) //pakt reference van een item en maakt alle plekken op het grid waar dat item stond weer leeg
    {
        for (int ix = 0; ix < item.itemData.width; ix++)
        {
            for (int iy = 0; iy < item.itemData.height; iy++)
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

    Vector2 positionOnGrid = new Vector2();
    Vector2Int tileGridPosition = new Vector2Int();

    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        //convert screen space naar rectangle, anders werkt het niet op andere resoluties/pcs
        Camera cam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas?.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            mousePosition,
            cam,
            out Vector2 localPoint
        );

        // Convert pivot-relative local point to top-left-relative (works for any pivot)
        localPoint.x -= rectTransform.rect.xMin;
        localPoint.y = rectTransform.rect.yMax - localPoint.y;

        tileGridPosition.x = (int)(localPoint.x / tileSizeWidth);
        tileGridPosition.y = (int)(localPoint.y / tileSizeHeight);

        return tileGridPosition;
    }

    public bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem) //places item MAAR checkt eerst
    {
        if (BoundaryCheck(posX, posY, inventoryItem.itemData.width, inventoryItem.itemData.height) == false) //checks of item in de grid past
        {
            Debug.Log("Item doesn't fit in the grid");
            return false;
        }

        if (OverlapCheck(posX, posY, inventoryItem.itemData.width, inventoryItem.itemData.height, ref overlapItem) == false) //checkt of item overlapt met een ander item
        {
            Debug.Log("Item overlaps with another item");
            overlapItem = null;
            return false;
        }

        if (overlapItem != null)
        {
            CleanGridReference(overlapItem);
        }

        RectTransform itemRectTransform = inventoryItem.GetComponent<RectTransform>();
        itemRectTransform.SetParent(rectTransform);

        inventoryItem.tileGridPosition = new Vector2Int(posX, posY);

        for (int x = 0; x < inventoryItem.itemData.width; x++)
        {
            for (int y = 0; y < inventoryItem.itemData.height; y++)
            {
                inventoryItemSlot[posX + x, posY + y] = inventoryItem; //actual plaatsen van het item in de grid array
            }
        }

        Vector2 position = new Vector2();
        position.x = posX * tileSizeWidth + tileSizeWidth * inventoryItem.itemData.width / 2;
        position.y = -(posY * tileSizeHeight + tileSizeHeight * inventoryItem.itemData.height / 2);

        itemRectTransform.localPosition = position;

        return true;
    }

    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem) //checkt voor overlap met ander item
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (inventoryItemSlot[posX + x, posY + y] != null)
                {
                    if (overlapItem == null)
                    {
                        overlapItem = inventoryItemSlot[posX + x, posY + y];
                    }
                    else
                    {
                        {
                            if (overlapItem != inventoryItemSlot[posX + x, posY + y])
                            {
                                return false; 
                            }
                        }
                    }
                }
            }
        }

        return true;
    }

    public bool PositionCheck(int posX, int posY) //checkt of een positie binnen de grenzen van het grid ligt (MATH)
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

    private bool BoundaryCheck(int posX, int posY, int width, int height) //checkt of een positie binnen de boundaries is
    {
        if (PositionCheck(posX, posY) == false) { return false; }

        posX += width-1;
        posY += height-1;

        if (PositionCheck(posX, posY) == false) { return false; }

        return true;
    }
}