using UnityEngine;
using UnityEngine.InputSystem;

// The arms rig must be a CHILD of the player camera (run Tools > Senna > Parent Arms To Camera).
// The camera itself provides the full pitch/yaw follow — the view can never rotate past the
// open edges of the mesh. This script only layers small, clamped offsets on top for feel.
//
// The core of the feel is the rotation lag: the arms trail every camera turn by a fraction
// (like the old 65%-follow setup) but the lag is hard-capped, so weight without clipping.
public class WeaponSway : MonoBehaviour
{
    [Header("Positional Sway")]
    // Deliberately tiny — position shift from looking around should be millimeters;
    // the rotation lag below carries the weight feel.
    [SerializeField] private float lookSwayAmount = 0.004f;
    [SerializeField] private float maxLookSwayAmount = 0.008f;
    [SerializeField] private float swaySmoothness = 8f;

    [Header("Rotation Lag")]
    // Fraction of every degree the camera turns that the arms trail behind,
    // spring-returned with a slight settle bounce. Capped by maxRotationLag.
    [SerializeField] private float rotationLagAmount = 0.18f;
    [SerializeField] private float maxRotationLag = 2.5f;
    [SerializeField] private float rotationLagSpring = 140f;
    [SerializeField] private float rotationLagDamping = 21f;
    [SerializeField] private float yawRollFactor = 0.3f;

    [Header("Strafe Sway")]
    [SerializeField] private float strafeSwayAmount = 0.03f;
    [SerializeField] private float strafeRollAngle = 1f;

    [Header("Camera Reference")]
    [SerializeField] private Transform cameraTransform;

    [Header("Pitch Framing")]
    // Composition at extreme angles: looking up pulls the arms down out of the sky view,
    // looking down tucks them slightly up toward the camera. Keep these small.
    [SerializeField] private float pitchFramingShift = 0.0012f;
    [SerializeField] private float maxPitchFramingShift = 0.05f;
    [SerializeField] private float pitchFramingSpeed = 10f;

    [Header("Head Bob Counter")]
    // The camera's head bob carries the arms with it (camera child). Countering it keeps
    // the gun steady in the world while the view bobs over it — the classic FPS walk feel.
    // 1 = gun fully steady (old sibling-setup look), 0 = gun glued to the view.
    [SerializeField] [Range(0f, 1f)] private float bobCounterAmount = 1f;

    [Header("Landing Kick")]
    [SerializeField] private float landingKickAmount = 0.05f;
    [SerializeField] private float jumpKickAmount = 0.03f;
    [SerializeField] private float landingKickSpring = 200f;
    [SerializeField] private float landingKickDamping = 16f;
    [SerializeField] private float jumpDriftScale = 0.008f;

    [Header("Recoil")]
    [SerializeField] private float recoilKickback = 0.05f;
    [SerializeField] private float recoilRotKick = 4f;
    [SerializeField] private float recoilSpring = 220f;
    [SerializeField] private float recoilDamping = 14f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference moveAction;

    [Header("ADS")]
    [SerializeField] private Vector3 aimPositionOffset = new Vector3(0f, 0.05f, 0.1f);
    [SerializeField] [Range(0f, 1f)] private float aimSwayDampen = 0.15f;

    private Vector3 _restPosition;
    private Quaternion _restRotation;
    private Vector2 _smoothedLook;
    private Vector2 _smoothedMove;
    private float _framingOffset;
    private Vector3 _cameraBob;
    private float _landingKickOffset;
    private float _landingKickVelocity;
    private float _jumpDrift;
    private float _externalVerticalVelocity;
    private float _recoilZOffset;
    private float _recoilZVelocity;
    private float _recoilPitchOffset;
    private float _recoilPitchVelocity;
    private float _prevCamPitch;
    private float _prevCamYaw;
    private float _lagPitch;
    private float _lagPitchVelocity;
    private float _lagYaw;
    private float _lagYawVelocity;
    private bool _lagInitialized;
    private float _aimBlend;
    private Vector3 _computedAimOffset;
    private bool _hasComputedOffset;

    private void Awake()
    {
        _restPosition = transform.localPosition;
        _restRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        lookAction.action.Enable();
        moveAction.action.Enable();
        _lagInitialized = false; // avoid a delta spike on (re)enable
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
        moveAction.action.Disable();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector2 look = lookAction.action.ReadValue<Vector2>();
        Vector2 move = moveAction.action.ReadValue<Vector2>();

        // Smoothed inputs: raw mouse deltas are jittery, WASD snaps 0/1 —
        // easing them in/out is most of what makes the sway feel organic
        _smoothedLook = Vector2.Lerp(_smoothedLook, look, swaySmoothness * dt);
        _smoothedMove = Vector2.Lerp(_smoothedMove, move, 6f * dt);

        // --- Camera angles (world, so body yaw is included) ---
        float camPitch = 0f, camYaw = 0f;
        if (cameraTransform != null)
        {
            Vector3 e = cameraTransform.eulerAngles;
            camPitch = e.x > 180f ? e.x - 360f : e.x;
            camYaw = e.y;
        }
        if (!_lagInitialized)
        {
            _prevCamPitch = camPitch;
            _prevCamYaw = camYaw;
            _lagInitialized = true;
        }
        float pitchDelta = Mathf.DeltaAngle(_prevCamPitch, camPitch);
        float yawDelta = Mathf.DeltaAngle(_prevCamYaw, camYaw);
        _prevCamPitch = camPitch;
        _prevCamYaw = camYaw;

        // --- Rotation lag: every camera turn injects displacement, the spring pulls it home.
        // Slightly underdamped on purpose — the small settle bounce reads as weight. ---
        _lagPitch = Mathf.Clamp(_lagPitch - pitchDelta * rotationLagAmount, -maxRotationLag, maxRotationLag);
        _lagYaw = Mathf.Clamp(_lagYaw - yawDelta * rotationLagAmount, -maxRotationLag, maxRotationLag);
        _lagPitchVelocity += (-rotationLagSpring * _lagPitch - rotationLagDamping * _lagPitchVelocity) * dt;
        _lagPitch += _lagPitchVelocity * dt;
        _lagYawVelocity += (-rotationLagSpring * _lagYaw - rotationLagDamping * _lagYawVelocity) * dt;
        _lagYaw += _lagYawVelocity * dt;

        // --- Landing kick ---
        _landingKickVelocity += (-landingKickSpring * _landingKickOffset - landingKickDamping * _landingKickVelocity) * dt;
        _landingKickOffset += _landingKickVelocity * dt;

        // --- Jump drift ---
        _jumpDrift = Mathf.Lerp(_jumpDrift, _externalVerticalVelocity * jumpDriftScale, 8f * dt);

        // --- Recoil ---
        _recoilZVelocity += (-recoilSpring * _recoilZOffset - recoilDamping * _recoilZVelocity) * dt;
        _recoilZOffset += _recoilZVelocity * dt;
        _recoilPitchVelocity += (-recoilSpring * _recoilPitchOffset - recoilDamping * _recoilPitchVelocity) * dt;
        _recoilPitchOffset += _recoilPitchVelocity * dt;

        // --- Pitch framing: positive pitch = looking down ---
        float targetFraming = Mathf.Clamp(camPitch * pitchFramingShift, -maxPitchFramingShift, maxPitchFramingShift);
        _framingOffset = Mathf.Lerp(_framingOffset, targetFraming, pitchFramingSpeed * dt);

        // --- Compose. Inputs are already smoothed or spring-driven, so apply directly:
        // an extra lerp here would just muffle the kicks. ---
        float swayScale = Mathf.Lerp(1f, aimSwayDampen, _aimBlend);
        float posX = Mathf.Clamp(-_smoothedLook.x * lookSwayAmount, -maxLookSwayAmount, maxLookSwayAmount) * swayScale;
        float posY = Mathf.Clamp(-_smoothedLook.y * lookSwayAmount, -maxLookSwayAmount, maxLookSwayAmount) * swayScale;
        float strafeOffset = -_smoothedMove.x * strafeSwayAmount * swayScale;

        Vector3 activeAimOffset = _hasComputedOffset ? _computedAimOffset : aimPositionOffset;
        Vector3 adsOffset = Vector3.Lerp(Vector3.zero, activeAimOffset, _aimBlend);
        transform.localPosition = _restPosition + adsOffset - _cameraBob * bobCounterAmount + new Vector3(
            posX + strafeOffset,
            posY + _framingOffset + _landingKickOffset + _jumpDrift,
            _recoilZOffset);

        float rotX = _lagPitch * swayScale - _recoilPitchOffset;
        float rotY = _lagYaw * swayScale;
        float rotZ = (-_lagYaw * yawRollFactor - _smoothedMove.x * strafeRollAngle) * swayScale;

        transform.localRotation = _restRotation * Quaternion.Euler(rotX, rotY, rotZ);
    }

    public void TriggerRecoil(float strength = 1f)
    {
        // Impulse scaled by the spring's natural frequency so recoilKickback and
        // recoilRotKick are the approximate peak offsets, independent of stiffness
        float omega = Mathf.Sqrt(recoilSpring);
        _recoilZVelocity -= recoilKickback * strength * omega;
        _recoilPitchVelocity += recoilRotKick * strength * omega;
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

    // Fed by SennaPlayerMovement each frame with the camera's current head-bob offset
    public void SetCameraBob(Vector3 bob)
    {
        _cameraBob = bob;
    }

    public void SetAimBlend(float t) => _aimBlend = t;

    // Called by SennaAimSystem.Start() when a scopePoint is assigned.
    // Overrides the manual aimPositionOffset with the exact delta needed to
    // bring the scope to camera center.
    public void SetAimOffset(Vector3 offset)
    {
        _computedAimOffset = offset;
        _hasComputedOffset = true;
    }
}
