using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public GameObject UISlot1;
    public GameObject UISlot2;
    public GameObject craftResult;

    public InventoryItem item1;
    public InventoryItem item2;

    public EquipmentSlot slotItem1;
    public EquipmentSlot slotItem2;

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

    }
}
