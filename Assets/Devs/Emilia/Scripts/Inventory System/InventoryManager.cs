using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public ItemGrid gridScript;
    public GridInteract interactScript;
    public GridController gridControllerScript;

    public GameObject inventoryGrid;
    public GameObject mainCamera;

    public TMP_Text guideTxt;

    void Start()
    {
        gridScript = inventoryGrid.GetComponent<ItemGrid>();
        interactScript = inventoryGrid.GetComponent<GridInteract>();
        gridControllerScript = mainCamera.GetComponent<GridController>();
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame) //CHANGE THIS TO INTERACT KEY LATER IN PLAYER ACTION MAP
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()
    {
        inventoryGrid.SetActive(!inventoryGrid.activeSelf);
        guideTxt.gameObject.SetActive(!guideTxt.gameObject.activeSelf);
    }
}
