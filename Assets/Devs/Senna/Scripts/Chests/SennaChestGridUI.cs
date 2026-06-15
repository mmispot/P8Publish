using UnityEngine;
using UnityEngine.InputSystem;

// Put this next to a SennaChest on the chest object: when the chest is
// opened (F, via SennaPlayerInteractor like every quest interactable) it
// shows BOTH the chest grid panel and the player inventory grid so items
// can be dragged between them, freezing the player exactly like
// InventoryPlayerBridge does for E. F or E closes both again. Drag the
// two panels from the Inventory canvas into the inspector slots.
// The frame stamps stop the same key press from being handled twice:
// every script reads Keyboard.current itself and execution order between
// them is undefined (see lessons.md).
public class SennaChestGridUI : MonoBehaviour
{
    [Header("Panels (from the Inventory canvas)")]
    [SerializeField] private GameObject chestGridPanel;   // CHEST panel holding the ChestGrid
    [SerializeField] private GameObject inventoryPanel;   // player's InventoryGrid

    [Header("Player")]
    [SerializeField] private SennaPlayerMovement playerMovement;
    [SerializeField] private SchootingRaycast shooting;

    private SennaChest _chest;
    private bool _open;
    private int _openedFrame = -1;
    private int _closedFrame = -1;

    private static int s_lastCloseFrame = -1;

    public bool IsOpen => _open;

    // While a chest owns the inventory panel, the chest closes everything on
    // E itself — InventoryManager and InventoryPlayerBridge must skip their
    // regular E toggle, including on the frame the chest closed (the same
    // press is visible to every script that frame).
    public static bool AnyOpen { get; private set; }
    public static bool InventoryToggleBlocked =>
        AnyOpen || Time.frameCount == s_lastCloseFrame;

    private void Awake()
    {
        // Auto-subscribe so no inspector event wiring is needed
        if (TryGetComponent(out _chest))
            _chest.onOpened.AddListener(Open);
    }

    private void OnDestroy()
    {
        if (_chest != null)
            _chest.onOpened.RemoveListener(Open);
        if (_open)
            AnyOpen = false; // destroyed mid-open (scene unload); don't leave E blocked
    }

    private void Update()
    {
        if (!_open || Time.timeScale == 0f || Time.frameCount == _openedFrame)
            return;

        if (Keyboard.current != null
            && (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            Close();
    }

    public void Open()
    {
        // The F press that closed the chest re-triggers the interactor when it
        // updates after us: Close() re-locked the cursor, so the interactor is
        // alive again in that same frame and the key still reads as pressed.
        // Without this guard the chest closes and reopens in one frame.
        if (_open || Time.frameCount == _closedFrame) return;
        _open = true;
        AnyOpen = true;
        _openedFrame = Time.frameCount;

        if (chestGridPanel != null) chestGridPanel.SetActive(true);
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
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
        AnyOpen = false;
        _closedFrame = Time.frameCount;
        s_lastCloseFrame = Time.frameCount;

        if (chestGridPanel != null) chestGridPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
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
