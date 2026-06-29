using UnityEngine;
using UnityEngine.Events;

public class SennaHealthPickup : MonoBehaviour, ISennaInteractable
{
    [SerializeField] private string displayName = "Med Kit";
    [SerializeField] private float healAmount = 25f;

    public UnityEvent onPickedUp;

    private bool _pickedUp;

    public string PromptText => $"[F] Pick up {displayName}";
    public bool CanInteract => !_pickedUp;

    public void Interact(GameObject interactor)
    {
        if (_pickedUp) return;

        var health = interactor.GetComponentInChildren<SennaPlayerHealth>()
                  ?? interactor.GetComponentInParent<SennaPlayerHealth>();

        if (health == null)
        {
            Debug.LogWarning("SennaHealthPickup: no SennaPlayerHealth found on interactor.");
            return;
        }

        if (health.CurrentHealth >= health.MaxHealth) return;

        _pickedUp = true;
        health.Heal(healAmount);
        onPickedUp?.Invoke();
        gameObject.SetActive(false);
    }
}
