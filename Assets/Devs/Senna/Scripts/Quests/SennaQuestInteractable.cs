using UnityEngine;
using UnityEngine.Events;

// Interactable that reports a key to SennaQuestManager when the player presses F.
// Use this for quests with SennaObjectiveType.Interact — the questKey here must
// match the interactKey on the corresponding SennaQuestObjective.
public class SennaQuestInteractable : MonoBehaviour, ISennaInteractable
{
    [SerializeField] private string promptText = "[F] Interact";
    [SerializeField] private string questKey;

    public UnityEvent onInteracted;

    private bool _used;

    public string PromptText => promptText;
    public bool CanInteract => !_used;

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;
        onInteracted?.Invoke();
        // Only mark used if the quest manager accepted the interaction.
        // This prevents permanently disabling the interactable if the player
        // presses F before its quest is active.
        var manager = SennaQuestManager.Instance;
        bool accepted = manager == null || manager.ReportInteractionCompleted(questKey);
        if (accepted) _used = true;
    }
}
