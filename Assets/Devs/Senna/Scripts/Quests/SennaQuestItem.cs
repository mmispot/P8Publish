using UnityEngine;
using UnityEngine.Events;

// World pickup for quest objectives. Needs a TRIGGER collider so the
// interactor's ray can hit it (QueryTriggerInteraction.Collide) while
// bullets (QueryTriggerInteraction.Ignore) pass straight through.
public class SennaQuestItem : MonoBehaviour, ISennaInteractable
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private string displayName; // falls back to the asset name when empty

    public UnityEvent onPickedUp; // SFX/VFX hook

    private string _prompt;

    public string PromptText => _prompt;
    public bool CanInteract => true;

    private void Awake()
    {
        string label = displayName;
        if (string.IsNullOrEmpty(label) && itemData != null)
            label = itemData.name;
        _prompt = "[F] Pick up " + label;
    }

    public void Interact(GameObject interactor)
    {
        SennaQuestManager.Instance?.ReportItemCollected(itemData);
        onPickedUp?.Invoke();
        gameObject.SetActive(false);
    }
}
