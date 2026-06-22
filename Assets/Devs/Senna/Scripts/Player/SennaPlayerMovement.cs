using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class SennaPlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float airControlMultiplier = 0.5f;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float verticalClamp = 85f;
    [SerializeField] private float downwardClamp = 80f;

    [Header("Head Bob")]
    [SerializeField] private float bobFrequency = 2.4f;
    [SerializeField] private float sprintBobFrequency = 3.4f;
    [SerializeField] private float bobAmplitude = 0.04f;

    [Header("Camera Effects")]
    [SerializeField] private float strafeTiltAngle = 1.5f;

    [Header("Crouch")]
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float crouchCameraDrop = 0.6f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float crouchBobMultiplier = 0.5f;
    [SerializeField] private CapsuleCollider bodyCollider;
    [SerializeField] private float crouchColliderHeight = 1f;

    [Header("Camera Recoil")]
    [SerializeField] private float recoilReturnSpring = 120f;
    [SerializeField] private float recoilReturnDamping = 12f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Landing")]
    [SerializeField] private float cameraLandingDip = 0.06f;
    [SerializeField] private float cameraJumpImpulse = 0.03f;
    [SerializeField] private float cameraLandingSpring = 180f;
    [SerializeField] private float cameraLandingDamping = 14f;
    [SerializeField] private float landingMinSpeed = 2f;
    [SerializeField] private float landingMaxSpeed = 10f;
    [SerializeField] private float cameraJumpDriftScale = 0.003f;
    [SerializeField] private WeaponSway weaponSway;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference crouchAction;

    private NavMeshAgent _agent;
    private Vector3 _smoothVelocity;
    private float _verticalRotation;
    private float _cameraRoll;
    private float _bobTimer;
    private Vector3 _cameraRestPos;
    private bool _movementEnabled = true;
    private bool _mouseLookEnabled = true;

    private float _verticalVelocity;
    private bool _isGrounded;
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    private float _cameraLandingOffset;
    private float _cameraLandingVelocity;
    private float _cameraJumpDrift;
    private Vector3 _currentBob;

    private bool _isCrouching;
    private float _crouchAmount;
    private float _standColliderHeight;
    private Vector3 _standColliderCenter;

    private float _recoilPitch;
    private float _recoilPitchVelocity;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.angularSpeed = 0f;

        if (cameraTransform != null)
            _cameraRestPos = cameraTransform.localPosition;

        if (bodyCollider != null)
        {
            _standColliderHeight = bodyCollider.height;
            _standColliderCenter = bodyCollider.center;
        }

        // Cursor state is owned entirely by GameStateManager (locks on Start/Resume/
        // Respawn, frees on the start/pause/death panels). The player must NOT grab the
        // cursor in Awake — in a scene where the player rig is left active behind the
        // start panel (e.g. the GunReload test scene) that hid the mouse and made the
        // Start button unclickable.
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        sprintAction.action.Enable();
        lookAction.action.Enable();
        jumpAction.action.Enable();
        crouchAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        sprintAction.action.Disable();
        lookAction.action.Disable();
        jumpAction.action.Disable();
        crouchAction?.action.Disable();
    }

    private void Update()
    {
        HandleCrouch();
        HandleJump();
        HandleMovement();
        HandleMouseLook();
        HandleCameraLanding();
        HandleHeadBob();
        HandleCameraEffects();
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;
        cameraTransform.localRotation = Quaternion.Euler(_verticalRotation + _recoilPitch, 0f, _cameraRoll);
    }

    private void HandleCrouch()
    {
        if (_movementEnabled && crouchAction != null && crouchAction.action.WasPressedThisFrame())
            _isCrouching = !_isCrouching;

        // Arms are a child of the camera, so they inherit the crouch drop automatically
        _crouchAmount = Mathf.Lerp(_crouchAmount, _isCrouching ? 1f : 0f, crouchTransitionSpeed * Time.deltaTime);

        if (bodyCollider != null)
        {
            float height = Mathf.Lerp(_standColliderHeight, crouchColliderHeight, _crouchAmount);
            Vector3 center = _standColliderCenter;
            center.y -= (_standColliderHeight - height) * 0.5f;
            bodyCollider.height = height;
            bodyCollider.center = center;
        }
    }

    private void HandleMouseLook()
    {
        if (!_mouseLookEnabled) return;

        Vector2 look = lookAction.action.ReadValue<Vector2>() * mouseSensitivity;
        _verticalRotation = Mathf.Clamp(_verticalRotation - look.y, -verticalClamp, downwardClamp);
        transform.Rotate(Vector3.up * look.x);
    }

    private void HandleMovement()
    {
        Vector2 input = _movementEnabled ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        bool sprinting = _movementEnabled && !_isCrouching && sprintAction.action.IsPressed();
        float targetSpeed = _isCrouching ? crouchSpeed : (sprinting ? sprintSpeed : walkSpeed);

        Vector3 target = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
        float rate = input.magnitude > 0.01f ? acceleration : deceleration;
        if (!_isGrounded) rate *= airControlMultiplier;
        _smoothVelocity = Vector3.Lerp(_smoothVelocity, target, rate * Time.deltaTime);

        // Keep agent in sync with transform, then apply our movement through NavMesh
        if (_agent.enabled)
        {
            _agent.nextPosition = transform.position;
            _agent.Move(_smoothVelocity * Time.deltaTime);
            transform.position = _agent.nextPosition;
        }
        else
        {
            transform.position += _smoothVelocity * Time.deltaTime;
        }
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null) return false;
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void HandleJump()
    {
        bool wasGrounded = _isGrounded;
        _isGrounded = _agent.enabled || (_verticalVelocity <= 0f && CheckGrounded());

        // Landing
        if (!wasGrounded && _isGrounded)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                transform.position = hit.position;
            _agent.enabled = true;

            float impact = Mathf.InverseLerp(landingMinSpeed, landingMaxSpeed, -_verticalVelocity);
            _cameraLandingVelocity -= cameraLandingDip * impact;
            weaponSway?.TriggerLandingKick(impact);
        }

        // Coyote time — grace window after walking off a ledge
        if (wasGrounded && !_isGrounded && _verticalVelocity <= 0f)
            _coyoteTimer = coyoteTime;
        _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.deltaTime);

        // Jump buffer — store input intent for a brief window
        if (jumpAction.action.WasPressedThisFrame())
            _jumpBufferTimer = jumpBufferTime;
        _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - Time.deltaTime);

        // Gravity
        if (!_isGrounded)
        {
            if (_verticalVelocity < 0f)
                _verticalVelocity -= gravity * fallMultiplier * Time.deltaTime;
            else if (_verticalVelocity > 0f && !jumpAction.action.IsPressed())
                _verticalVelocity -= gravity * lowJumpMultiplier * Time.deltaTime;
            else
                _verticalVelocity -= gravity * Time.deltaTime;
        }
        else
            _verticalVelocity = 0f;

        // Jump — fires from buffer, respects coyote window
        if (_jumpBufferTimer > 0f && (_isGrounded || _coyoteTimer > 0f) && _movementEnabled)
        {
            _jumpBufferTimer = 0f;

            if (_isCrouching)
            {
                // First jump press while crouched stands you up instead of jumping
                _isCrouching = false;
            }
            else
            {
                _verticalVelocity = jumpForce;
                _isGrounded = false;
                _agent.enabled = false;
                _coyoteTimer = 0f;

                _cameraLandingVelocity += cameraJumpImpulse;
                weaponSway?.TriggerJumpKick();
            }
        }

        if (!_isGrounded)
            transform.position += Vector3.up * _verticalVelocity * Time.deltaTime;

        weaponSway?.SetVerticalVelocity(_isGrounded ? 0f : _verticalVelocity);
    }

    private void HandleCameraLanding()
    {
        float spring = -cameraLandingSpring * _cameraLandingOffset;
        float damp = -cameraLandingDamping * _cameraLandingVelocity;
        _cameraLandingVelocity += (spring + damp) * Time.deltaTime;
        _cameraLandingOffset += _cameraLandingVelocity * Time.deltaTime;

        float driftTarget = _isGrounded ? 0f : _verticalVelocity * cameraJumpDriftScale;
        _cameraJumpDrift = Mathf.Lerp(_cameraJumpDrift, driftTarget, 8f * Time.deltaTime);

        // Recoil pitch springs back to zero (slightly underdamped for a small settle bounce)
        float recoilSpring = -recoilReturnSpring * _recoilPitch;
        float recoilDamp = -recoilReturnDamping * _recoilPitchVelocity;
        _recoilPitchVelocity += (recoilSpring + recoilDamp) * Time.deltaTime;
        _recoilPitch += _recoilPitchVelocity * Time.deltaTime;
    }

    private void HandleHeadBob()
    {
        if (cameraTransform == null) return;

        float speed = new Vector3(_smoothVelocity.x, 0f, _smoothVelocity.z).magnitude;
        Vector3 landingDip = new Vector3(0f, _cameraLandingOffset + _cameraJumpDrift - crouchCameraDrop * _crouchAmount, 0f);

        Vector3 targetBob = Vector3.zero;
        float smoothing = 8f;

        if (speed >= 0.3f && _isGrounded)
        {
            bool isSprinting = sprintAction.action.IsPressed() && _isGrounded && !_isCrouching;
            float currentBobFrequency = isSprinting ? sprintBobFrequency : bobFrequency;
            float currentBobAmplitude = bobAmplitude * Mathf.Lerp(1f, crouchBobMultiplier, _crouchAmount);
            _bobTimer += Time.deltaTime * currentBobFrequency * (speed / walkSpeed);

            targetBob = new Vector3(
                Mathf.Sin(_bobTimer * 0.5f) * currentBobAmplitude * 0.5f,
                Mathf.Abs(Mathf.Sin(_bobTimer)) * currentBobAmplitude,
                0f);
            smoothing = 12f;
        }
        else
        {
            _bobTimer = 0f;
        }

        _currentBob = Vector3.Lerp(_currentBob, targetBob, smoothing * Time.deltaTime);
        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition, _cameraRestPos + _currentBob + landingDip, smoothing * Time.deltaTime);

        // The arms are a camera child and inherit the bob — WeaponSway counters it
        // so the gun stays steady in the world while the view bobs over it
        weaponSway?.SetCameraBob(_currentBob);
    }

    private void HandleCameraEffects()
    {
        Vector2 input = _movementEnabled ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        _cameraRoll = Mathf.Lerp(_cameraRoll, -input.x * strafeTiltAngle, 8f * Time.deltaTime);
    }

    // Kick the camera pitch up; the spring in HandleCameraLanding brings it back down
    public void AddRecoil(float pitchKick)
    {
        _recoilPitch -= pitchKick;
    }

    public void EnableMovement() => _movementEnabled = true;
    public void DisableMovement() { _movementEnabled = false; _smoothVelocity = Vector3.zero; }
    public void EnableMouseLook() => _mouseLookEnabled = true;
    public void DisableMouseLook() => _mouseLookEnabled = false;
}
