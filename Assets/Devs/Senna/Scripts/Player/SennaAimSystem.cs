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

    [Header("Input")]
    [SerializeField] private InputAction aimAction =
        new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");

    private float _aimBlend;

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
