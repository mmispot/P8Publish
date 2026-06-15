using UnityEngine;

// Anything the player can target with SennaPlayerInteractor:
// quest pickups, levers, terminals. One interface so a single
// interactor and prompt UI handle both pickup and interaction prompts.
public interface ISennaInteractable
{
    string PromptText { get; }
    bool CanInteract { get; }
    void Interact(GameObject interactor);
}
