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
    public PlayerMovement player;

    void Start()
    {
        gridScript = inventoryGrid.GetComponent<ItemGrid>();
        interactScript = inventoryGrid.GetComponent<GridInteract>();
        gridControllerScript = mainCamera.GetComponent<GridController>();

        if (player != null)
        {
            player = player.GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()
    {
        bool isOpen = !inventoryGrid.activeSelf;
        inventoryGrid.SetActive(isOpen);
        guideTxt.gameObject.SetActive(isOpen);

        if (isOpen)
        {
            player.DisableMovement();
            player.DisableMouseLook();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            player.EnableMovement();
            player.EnableMouseLook();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}