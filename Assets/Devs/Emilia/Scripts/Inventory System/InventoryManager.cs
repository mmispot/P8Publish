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

    public GameObject player;
    //[SerializeField] private PlayerMovement playerMovement;
    //[SerializeField] private MouseLook playerMouseLook;

    void Start()
    {
        gridScript = inventoryGrid.GetComponent<ItemGrid>();
        interactScript = inventoryGrid.GetComponent<GridInteract>();
        gridControllerScript = mainCamera.GetComponent<GridController>();

        //playerMovement = player.GetComponent<PlayerMovement>();
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
            //stop movement when open
        }
        else
        {
            //resume movement when closed
        }
    }
}