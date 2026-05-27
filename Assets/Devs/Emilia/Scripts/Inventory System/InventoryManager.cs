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
    //[SerializeField] private PlayerMovement playerMovement;
    //[SerializeField] private MouseLook playerMouseLook;
    

    void Start()
    {
        gridScript = inventoryGrid.GetComponent<ItemGrid>();
        interactScript = inventoryGrid.GetComponent<GridInteract>();
        gridControllerScript = mainCamera.GetComponent<GridController>();
        player = player.GetComponent<PlayerMovement>();
        //playerMouseLook = player.GetComponent<MouseLook>();
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
        }
        else
        {
            player.EnableMovement();
        }
    }
}