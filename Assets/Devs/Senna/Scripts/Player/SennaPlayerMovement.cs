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

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.angularSpeed = 0f;

        if (cameraTransform != null)
            _cameraRestPos = cameraTransform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        sprintAction.action.Enable();
        lookAction.action.Enable();
        jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        sprintAction.action.Disable();
        lookAction.action.Disable();
        jumpAction.action.Disable();
    }

    private void Update()
    {
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
        cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, _cameraRoll);
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
        bool sprinting = _movementEnabled && sprintAction.action.IsPressed();
        float targetSpeed = sprinting ? sprintSpeed : walkSpeed;

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
            _verticalVelocity = jumpForce;
            _isGrounded = false;
            _agent.enabled = false;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;

            _cameraLandingVelocity += cameraJumpImpulse;
            weaponSway?.TriggerJumpKick();
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
    }

    private void HandleHeadBob()
    {
        if (cameraTransform == null) return;

        float speed = new Vector3(_smoothVelocity.x, 0f, _smoothVelocity.z).magnitude;
        Vector3 landingDip = new Vector3(0f, _cameraLandingOffset + _cameraJumpDrift, 0f);

        if (speed < 0.3f || !_isGrounded)
        {
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition, _cameraRestPos + landingDip, 8f * Time.deltaTime);
            _bobTimer = 0f;
            return;
        }

        bool isSprinting = sprintAction.action.IsPressed() && _isGrounded;
        float currentBobFrequency = isSprinting ? sprintBobFrequency : bobFrequency;
        _bobTimer += Time.deltaTime * currentBobFrequency * (speed / walkSpeed);

        Vector3 bob = new Vector3(
            Mathf.Sin(_bobTimer * 0.5f) * bobAmplitude * 0.5f,
            Mathf.Abs(Mathf.Sin(_bobTimer)) * bobAmplitude,
            0f);

        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition, _cameraRestPos + bob + landingDip, 12f * Time.deltaTime);
    }

    private void HandleCameraEffects()
    {
        Vector2 input = _movementEnabled ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        _cameraRoll = Mathf.Lerp(_cameraRoll, -input.x * strafeTiltAngle, 8f * Time.deltaTime);
    }

    public void EnableMovement() => _movementEnabled = true;
    public void DisableMovement() { _movementEnabled = false; _smoothVelocity = Vector3.zero; }
    public void EnableMouseLook() => _mouseLookEnabled = true;
    public void DisableMouseLook() => _mouseLookEnabled = false;
}
