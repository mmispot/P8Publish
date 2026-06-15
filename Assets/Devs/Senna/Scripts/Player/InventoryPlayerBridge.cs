using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryPlayerBridge : MonoBehaviour
{
    [SerializeField] private SennaPlayerMovement playerMovement;
    [SerializeField] private SchootingRaycast shooting;

    private bool _inventoryOpen = true;

    private void Update()
    {
        // The chest UI owns the cursor/movement while it's open and closes
        // everything on E itself — don't fight it (see SennaChestGridUI).
        if (SennaChestGridUI.InventoryToggleBlocked) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            Toggle();
    }

    private void Toggle()
    {
        _inventoryOpen = !_inventoryOpen;

        if (_inventoryOpen)
        {
            playerMovement?.DisableMovement();
            playerMovement?.DisableMouseLook();
            shooting?.DisableShoot();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            playerMovement?.EnableMovement();
            playerMovement?.EnableMouseLook();
            shooting?.EnableShoot();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
