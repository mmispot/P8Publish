using UnityEngine;
using UnityEngine.InputSystem;

// Opens Emilia's CHEST panel (the ChestGrid) when a SennaChest fires
// onOpened, freezing the player exactly like InventoryPlayerBridge does
// for E. F closes it again. The opened-frame guard stops the same F press
// that opened the chest from instantly closing it (SennaPlayerInteractor's
// update order relative to ours is undefined).
public class SennaChestGridUI : MonoBehaviour
{
    [SerializeField] private GameObject chestPanel; // CHEST under the Inventory canvas
    [SerializeField] private SennaPlayerMovement playerMovement;
    [SerializeField] private SchootingRaycast shooting;

    private bool _open;
    private int _openedFrame = -1;

    public bool IsOpen => _open;

    private void Update()
    {
        if (!_open || Time.timeScale == 0f || Time.frameCount == _openedFrame)
            return;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            Close();
    }

    // Wired to SennaChest.onOpened by Tools > Senna > Setup Chest
    public void Open()
    {
        if (_open) return;
        _open = true;
        _openedFrame = Time.frameCount;

        chestPanel.SetActive(true);
        if (playerMovement != null)
        {
            playerMovement.DisableMovement();
            playerMovement.DisableMouseLook();
        }
        if (shooting != null) shooting.DisableShoot();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (!_open) return;
        _open = false;

        chestPanel.SetActive(false);
        if (playerMovement != null)
        {
            playerMovement.EnableMovement();
            playerMovement.EnableMouseLook();
        }
        if (shooting != null) shooting.EnableShoot();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
