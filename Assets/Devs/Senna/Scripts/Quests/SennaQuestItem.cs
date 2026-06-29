using UnityEngine;
using UnityEngine.Events;

public class SennaQuestItem : MonoBehaviour, ISennaInteractable
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private string displayName;
    [SerializeField] private GridController gridController;

    public UnityEvent onPickedUp;

    private string _prompt;

    public string PromptText => _prompt;
    public bool CanInteract
    {
        get
        {
            var manager = SennaQuestManager.Instance;
            // No manager = test scene, allow everything. Otherwise only show the
            // prompt when this item is part of the currently active quest.
            return manager == null || manager.IsItemCollectable(itemData);
        }
    }

    private void Awake()
    {
        string label = displayName;
        if (string.IsNullOrEmpty(label) && itemData != null)
            label = itemData.name;
        _prompt = "[F] Pick up " + label;
    }

    public void Interact(GameObject interactor)
    {
        AddToInventory();
        SennaQuestManager.Instance?.ReportItemCollected(itemData);
        onPickedUp?.Invoke();
    }

    private void AddToInventory()
    {
        GridController gc = gridController;
        if (gc == null)
            gc = Object.FindFirstObjectByType<GridController>(FindObjectsInactive.Include);
        if (gc == null)
        {
            Debug.LogWarning("SennaQuestItem: No GridController found in scene.");
            gameObject.SetActive(false);
            return;
        }

        ItemGrid grid = Object.FindFirstObjectByType<ItemGrid>(FindObjectsInactive.Include);
        if (grid != null)
            grid.EnsureInitialized();

        var go = Object.Instantiate(gc.ItemPrefab, gc.CanvasTransform);
        var invItem = go.GetComponent<InventoryItem>();
        invItem.Set(itemData);

        // Placed items need raycasts enabled so the player can drag them.
        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = true;

        gc.InsertItem(invItem, grid);

        // Only disable after successful inventory insertion.
        // If inventory is full, InsertItem leaves the item held in hand —
        // the world object still disappears since the player "grabbed" it.
        gameObject.SetActive(false);
    }
}