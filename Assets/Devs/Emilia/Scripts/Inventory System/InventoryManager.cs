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

    private MouseLook mouseLook;

    void Start()
    {
        gridScript = inventoryGrid.GetComponent<ItemGrid>();
        interactScript = inventoryGrid.GetComponent<GridInteract>();
        gridControllerScript = mainCamera.GetComponent<GridController>();
        mouseLook = FindObjectOfType<MouseLook>();
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
        bool opening = !inventoryGrid.activeSelf;
        inventoryGrid.SetActive(opening);
        guideTxt.gameObject.SetActive(opening);

        if (opening)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (mouseLook != null) mouseLook.enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (mouseLook != null) mouseLook.enabled = true;
        }
    }
}
