using UnityEngine;
using UnityEngine.InputSystem;

// Center-screen raycast targeting for pickups/interactables, mirroring how
// shooting aims. Quest pickups use trigger colliders: visible to this ray
// (Collide) but invisible to bullets (SchootingRaycast raycasts with Ignore).
// F is read straight from Keyboard.current — same as Esc in GameStateManager
// and E in InventoryPlayerBridge — so Dominik's shared .inputactions stays untouched.
public class SennaPlayerInteractor : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float range = 3.5f;
    [SerializeField] private LayerMask interactMask = ~0;

    // Prompt of whatever is currently targeted; null when nothing is.
    // Targets cache their prompt strings, so this only changes by reference.
    public string CurrentPromptText { get; private set; }

    private ISennaInteractable _target;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        // Dead while paused, on the death screen, or before Start is pressed
        if (Time.timeScale == 0f)
        {
            SetTarget(null);
            return;
        }

        UpdateTarget();

        if (_target != null && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            _target.Interact(gameObject);
            SetTarget(null); // re-acquire next frame; pickups deactivate themselves
        }
    }

    private void UpdateTarget()
    {
        ISennaInteractable found = null;

        if (playerCamera != null)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, range, interactMask, QueryTriggerInteraction.Collide)
                && hit.collider.TryGetComponent(out ISennaInteractable interactable)
                && interactable.CanInteract)
            {
                found = interactable;
            }
        }

        if (!ReferenceEquals(found, _target))
            SetTarget(found);
    }

    private void SetTarget(ISennaInteractable target)
    {
        _target = target;
        CurrentPromptText = target != null ? target.PromptText : null;
    }
}
