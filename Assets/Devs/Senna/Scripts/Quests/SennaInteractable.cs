using UnityEngine;
using UnityEngine.Events;

// Generic interactable (levers, terminals, doors): shows a prompt
// and fires a UnityEvent when the player presses F on it.
public class SennaInteractable : MonoBehaviour, ISennaInteractable
{
    [SerializeField] private string promptText = "[F] Interact";
    [SerializeField] private bool singleUse;

    public UnityEvent onInteracted;

    private bool _used;

    public string PromptText => promptText;
    public bool CanInteract => !singleUse || !_used;

    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;
        _used = true;
        onInteracted?.Invoke();
    }
}
