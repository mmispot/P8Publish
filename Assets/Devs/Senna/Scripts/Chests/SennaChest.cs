using UnityEngine;
using UnityEngine.Events;

// World chest: targeted by SennaPlayerInteractor and opened with F like
// every other ISennaInteractable. All config lives in the SennaChestData
// asset; this component only holds per-instance opened state and the
// lid-animation/SFX hook. Needs a TRIGGER collider so the interactor's
// ray can hit it while bullets pass through (same as SennaQuestItem).
public class SennaChest : MonoBehaviour, ISennaInteractable
{
    [SerializeField] private SennaChestData chestData;

    public UnityEvent onOpened; // lid animation / SFX / VFX hook

    private string _prompt;
    private bool _opened;

    public SennaChestData ChestData => chestData;
    public bool IsOpened => _opened;

    public string PromptText => _prompt;
    public bool CanInteract => !_opened || (chestData != null && !chestData.openOnce);

    private void Awake()
    {
        if (chestData == null)
        {
            Debug.LogWarning($"SennaChest on '{name}' has no SennaChestData assigned.", this);
            _prompt = "[F] Open chest";
            return;
        }

        _prompt = !string.IsNullOrEmpty(chestData.promptText)
            ? chestData.promptText
            : "[F] Open " + chestData.chestName;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;
        _opened = true;
        // Loot handout slots in here once the inventory integration lands
        // (chestData.lootItems -> InventoryManager).
        onOpened?.Invoke();
    }
}
