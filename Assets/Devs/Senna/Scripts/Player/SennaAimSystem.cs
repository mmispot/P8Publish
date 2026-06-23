using UnityEngine;
using UnityEngine.InputSystem;

public class SennaAimSystem : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private WeaponSway weaponSway;
    [SerializeField] private SennaPlayerMovement playerMovement;

    [Header("ADS Feel")]
    [SerializeField] private float aimBlendSpeed = 8f;

    [Header("FOV")]
    [SerializeField] private float normalFov = 90f;
    [SerializeField] private float aimFov = 65f;

    [Header("Scope Point")]
    // Place an empty GameObject at the iron sight / scope of the gun and assign it here.
    // The system computes the exact offset needed to bring that point to camera center.
    // Leave empty to fall back to the manual Aim Position Offset on WeaponSway.
    [SerializeField] private Transform scopePoint;

    [Header("Input")]
    [SerializeField] private InputAction aimAction =
        new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");

    private float _aimBlend;

    private void Start()
    {
        if (scopePoint != null && weaponSway != null && playerCamera != null)
        {
            // Offset needed to bring scopePoint to camera center (in camera local space).
            // The arms rig is a child of the camera, so adding this offset to _restPosition
            // slides the entire rig until the scope is at the camera's eye point.
            Vector3 offset = -playerCamera.transform.InverseTransformPoint(scopePoint.position);
            weaponSway.SetAimOffset(offset);
        }
    }

    private void OnEnable()
    {
        aimAction.Enable();
    }

    private void OnDisable()
    {
        aimAction.Disable();
        _aimBlend = 0f;
        Apply(0f);
    }

    private void Update()
    {
        float target = aimAction.IsPressed() ? 1f : 0f;
        _aimBlend = Mathf.Lerp(_aimBlend, target, aimBlendSpeed * Time.deltaTime);
        Apply(_aimBlend);
    }

    private void Apply(float blend)
    {
        if (playerCamera != null)
            playerCamera.fieldOfView = Mathf.Lerp(normalFov, aimFov, blend);

        weaponSway?.SetAimBlend(blend);
        playerMovement?.SetAimBlend(blend);
    }
}
