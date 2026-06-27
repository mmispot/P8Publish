using UnityEngine;
using UnityEngine.Events;

// World pickup for a gun. Place on a GameObject with a trigger collider.
// Assign playerGun to the disabled gun GameObject on the player rig — pressing
// F enables it and hides this world prop. Hooks into SennaPlayerInteractor via
// ISennaInteractable, same as all other pickups in this project.
public class SennaGunPickup : MonoBehaviour, ISennaInteractable
{
    [SerializeField] private SennaGunData gunData;

    [Header("Player Gun")]
    [Tooltip("The disabled gun GameObject on the player rig that gets enabled on pickup.")]
    [SerializeField] private GameObject playerGun;

    [Header("Quest")]
    [Tooltip("If set, reports this key to SennaQuestManager when the gun is picked up.")]
    [SerializeField] private string questInteractKey;

    public UnityEvent onPickedUp;

    private bool _pickedUp;

    public string PromptText => gunData != null ? $"[F] Pick up {gunData.displayName}" : "[F] Pick up Gun";
    public bool CanInteract => !_pickedUp;

    public void Interact(GameObject interactor)
    {
        if (_pickedUp) return;
        _pickedUp = true;

        if (playerGun != null)
            playerGun.SetActive(true);
        else
            Debug.LogWarning("SennaGunPickup: playerGun not assigned.");

        if (!string.IsNullOrEmpty(questInteractKey))
            SennaQuestManager.Instance?.ReportInteractionCompleted(questInteractKey);

        onPickedUp?.Invoke();
        gameObject.SetActive(false);
    }
}
