using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public GameObject UISlot1;
    public GameObject UISlot2;
    public ItemGrid craftResultGrid;

    public InventoryItem item1;
    public InventoryItem item2;
    public InventoryItem finalItem;

    public EquipmentSlot slotItem1;
    public EquipmentSlot slotItem2;

    public List<ItemData> possibleItemData = new List<ItemData>();

    public GameObject itemPrefab;

    public void Start()
    {
        slotItem1 = UISlot1.GetComponent<EquipmentSlot>();
        slotItem2 = UISlot2.GetComponent<EquipmentSlot>();
    }

    public void Update()
    {
        item1 = slotItem1.equippedItem;
        item2 = slotItem2.equippedItem;

        Debug.Log($"CraftingManager Update: item1 = {(item1 != null ? item1.itemData.itemName : "null")}, item2 = {(item2 != null ? item2.itemData.itemName : "null")}");
    }

    public void TryCraft()
    {
        if (item1 == null || item2 == null) return;

        if (item1.itemData.itemName == "Scrap" && item2.itemData.itemName == "Scrap" && finalItem == null)
        {
            ItemData resultData = possibleItemData[0];

            ClearItemData();
            CraftResult(resultData);

        }
    }

    private void ClearItemData()
    {
        slotItem1.equippedItem = null;
        slotItem2.equippedItem = null;

        Destroy(item1.gameObject);
        Destroy(item2.gameObject);
    }

    public void CraftResult(ItemData item)
    {
        if (finalItem != null)
        {
            Destroy(finalItem.gameObject);
            finalItem = null;
        }

        GameObject newItem = Instantiate(itemPrefab); // no parent, let PlaceItem handle it
        InventoryItem inventoryItem = newItem.GetComponent<InventoryItem>();
        inventoryItem.Set(item);

        CanvasGroup cg = newItem.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        craftResultGrid.PlaceItem(inventoryItem, 0, 0);
        finalItem = inventoryItem;
    }
}
