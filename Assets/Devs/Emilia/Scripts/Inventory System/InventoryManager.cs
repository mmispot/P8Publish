using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public ItemGrid gridScript;
    public GridInteract interactScript;
    public GridController gridControllerScript;

    public GameObject inventoryGrid;
    public GameObject mainCamera;

    void Start()
    {
        gridScript = inventoryGrid.GetComponent<ItemGrid>();
        interactScript = inventoryGrid.GetComponent<GridInteract>();
        gridControllerScript = mainCamera.GetComponent<GridController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) //CHANGE THIS TO INTERACT KEY LATER IN PLAYER ACTION MAP
        {
            inventoryGrid.SetActive(!inventoryGrid.activeSelf);
        }
    }
}
