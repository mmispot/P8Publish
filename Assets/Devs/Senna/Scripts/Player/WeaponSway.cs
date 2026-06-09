using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSway : MonoBehaviour
{
    [Header("Positional Sway")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float maxSwayAmount = 0.06f;
    [SerializeField] private float swaySmoothness = 8f;

    [Header("Rotational Sway")]
    [SerializeField] private float rotSwayAmount = 3f;
    [SerializeField] private float maxRotSway = 5f;
    [SerializeField] private float rotSwaySmoothness = 8f;

    [Header("Strafe Sway")]
    [SerializeField] private float strafeSwayAmount = 0.03f;
    [SerializeField] private float strafeSwaySmoothness = 6f;

    [Header("Camera Follow")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float pitchFollowSpeed = 25f;
    [SerializeField] [Range(0f, 1f)] private float pitchFollowAmount = 0.65f;

    [Header("Pitch Position Offset")]
    [SerializeField] private float pitchPositionShift = 0.006f;
    [SerializeField] private float maxPitchPositionShift = 0.22f;
    [SerializeField] private float pitchPositionSpeed = 10f;

    [Header("Landing Kick")]
    [SerializeField] private float landingKickAmount = 0.05f;
    [SerializeField] private float jumpKickAmount = 0.03f;
    [SerializeField] private float landingKickSpring = 200f;
    [SerializeField] private float landingKickDamping = 16f;
    [SerializeField] private float jumpDriftScale = 0.008f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference moveAction;

    private Vector3 _restPosition;
    private Quaternion _restRotation;
    private float _currentPitch;
    private float _currentPitchOffset;
    private float _landingKickOffset;
    private float _landingKickVelocity;
    private float _jumpDrift;
    private float _externalVerticalVelocity;

    private void Awake()
    {
        _restPosition = transform.localPosition;
        _restRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        lookAction.action.Enable();
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
        moveAction.action.Disable();
    }

    private void Update()
    {
        Vector2 look = lookAction.action.ReadValue<Vector2>();
        Vector2 move = moveAction.action.ReadValue<Vector2>();

        // Read camera pitch once — used by both position and rotation
        float cameraPitch = 0f;
        if (cameraTransform != null)
        {
            cameraPitch = cameraTransform.localEulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;
        }

        // --- Landing kick ---
        float kickSpring = -landingKickSpring * _landingKickOffset;
        float kickDamp = -landingKickDamping * _landingKickVelocity;
        _landingKickVelocity += (kickSpring + kickDamp) * Time.deltaTime;
        _landingKickOffset += _landingKickVelocity * Time.deltaTime;

        // --- Jump drift ---
        _jumpDrift = Mathf.Lerp(_jumpDrift, _externalVerticalVelocity * jumpDriftScale, 8f * Time.deltaTime);

        // --- Position ---
        float posX = Mathf.Clamp(-look.x * swayAmount, -maxSwayAmount, maxSwayAmount);
        float posY = Mathf.Clamp(-look.y * swayAmount, -maxSwayAmount, maxSwayAmount);
        float strafeOffset = -move.x * strafeSwayAmount;

        // Pitch position shift: looking up pulls arms down, looking down pushes arms up
        // This keeps the arms in frame at extreme angles (AAA trick)
        float targetPitchOffset = Mathf.Clamp(-cameraPitch * pitchPositionShift, -maxPitchPositionShift, maxPitchPositionShift);
        _currentPitchOffset = Mathf.Lerp(_currentPitchOffset, targetPitchOffset, pitchPositionSpeed * Time.deltaTime);

        Vector3 targetPos = _restPosition + new Vector3(posX + strafeOffset, posY + _currentPitchOffset + _landingKickOffset + _jumpDrift, 0f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, swaySmoothness * Time.deltaTime);

        // --- Rotation ---
        float rotX = Mathf.Clamp(look.y * rotSwayAmount, -maxRotSway, maxRotSway);
        float rotY = Mathf.Clamp(look.x * rotSwayAmount, -maxRotSway, maxRotSway);
        float rotZ = Mathf.Clamp(-look.x * rotSwayAmount, -maxRotSway, maxRotSway);

        // Partial pitch follow — arms rotate with camera but less, keeping hands visible
        _currentPitch = Mathf.Lerp(_currentPitch, cameraPitch * pitchFollowAmount, pitchFollowSpeed * Time.deltaTime);
        rotX += _currentPitch;

        Quaternion targetRot = _restRotation * Quaternion.Euler(rotX, rotY, rotZ);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, rotSwaySmoothness * Time.deltaTime);
    }

    public void TriggerLandingKick(float impact)
    {
        _landingKickVelocity -= landingKickAmount * impact;
    }

    public void TriggerJumpKick()
    {
        _landingKickVelocity += jumpKickAmount;
    }

    public void SetVerticalVelocity(float velocity)
    {
        _externalVerticalVelocity = velocity;
    }
}
