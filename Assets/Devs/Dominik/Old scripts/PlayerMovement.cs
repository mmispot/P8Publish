using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Walk / Sprint")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float smoothTime = 0.05f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float airControlFactor = 0.3f;
    [SerializeField] private float landingSnapRadius = 1.5f;
    [SerializeField] private float jumpGracePeriod = 0.1f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;

    [Header("References")]
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private SchootingRaycast shooting;

    private NavMeshAgent _agent;

    private Vector2 _moveInput;
    private bool _isSprinting;

    private Vector3 _smoothVelRef;
    private Vector3 _horizontalVel;

    private bool _isAirborne;
    private float _verticalVel;
    private float _airborneTimer;

    public bool IsGrounded => !_isAirborne;
    public bool IsMoving => _moveInput.sqrMagnitude > 0.01f;
    public bool IsSprinting => _isSprinting;

    private void Awake()
    {
        //get the navmesh agent and configure it
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        _agent.autoBraking = false;
        _agent.autoTraverseOffMeshLink = false;
        _agent.acceleration = 50f;
        // updatePosition must stay true — agent.Move() only moves the transform when this is on
    }

    private void Start()
    {
        // ensure the agent is on the navmesh at spawn — if the player spawns even slightly
        // above the surface, isOnNavMesh stays false and Move() silently does nothing
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            _agent.Warp(hit.position);
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        jumpAction?.action.Enable();
        sprintAction?.action.Enable();

        if (jumpAction != null)
            jumpAction.action.performed += HandleJump;

        // Re-snap to NavMesh in case the agent was warped/disabled since Start()
        if (_agent != null && !_agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                _agent.Warp(hit.position);
        }
    }

    private void OnDisable()
    {
        //disable all input actions and unsubscribe from jump
        moveAction?.action.Disable();
        jumpAction?.action.Disable();
        sprintAction?.action.Disable();

        if (jumpAction != null)
            jumpAction.action.performed -= HandleJump;
    }

    private void Update()
    {
        //read input then run either grounded or airborne movement
        ReadInput();

        if (_isAirborne)
            UpdateAirborne(Time.deltaTime);
        else
            UpdateGrounded(Time.deltaTime);
    }

    private void ReadInput()
    {
        //read move and sprint input each frame
        _moveInput = moveAction?.action.ReadValue<Vector2>() ?? Vector2.zero;
        _isSprinting = sprintAction?.action.IsPressed() ?? false;
    }

    private void HandleJump(InputAction.CallbackContext ctx)
    {
        //jump only when grounded and on the navmesh
        if (_isAirborne || !_agent.isOnNavMesh || !_agent.enabled)
            return;

        _verticalVel = jumpForce;
        _isAirborne = true;
        _airborneTimer = 0f;
        _agent.enabled = false;
    }

    private void UpdateGrounded(float dt)
    {
        Vector3 targetVel = BuildTargetVelocity();

        _horizontalVel = Vector3.SmoothDamp(
            _horizontalVel, targetVel,
            ref _smoothVelRef, smoothTime);

        if (_horizontalVel.sqrMagnitude < 0.001f)
        {
            _horizontalVel = Vector3.zero;
            _agent.velocity = Vector3.zero;
        }

        if (_agent.isOnNavMesh && _agent.enabled)  // <-- guard added here so navmesh works even if the player spawns slightly above the surface
            _agent.Move(_horizontalVel * dt);

        if (mouseLook != null)
            transform.rotation = Quaternion.Euler(0f, mouseLook.GetYaw(), 0f);
    }

    private void UpdateAirborne(float dt)
    {
        //apply gravity, air steering and move the transform manually
        _airborneTimer += dt;
        _verticalVel += Physics.gravity.y * dt;

        _horizontalVel = Vector3.Lerp(
            _horizontalVel,
            BuildTargetVelocity(),
            airControlFactor * dt);

        transform.position += (_horizontalVel + Vector3.up * _verticalVel) * dt;

        TryLand();
    }

    private void TryLand()
    {
        //snap back onto the navmesh once falling and close enough to the surface
        bool pastGracePeriod = _airborneTimer > jumpGracePeriod;
        bool isFalling = _verticalVel < 0f;

        if (!pastGracePeriod || !isFalling)
            return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, landingSnapRadius, NavMesh.AllAreas))
        {
            _agent.enabled = true;
            _agent.Warp(hit.position);
            _isAirborne = false;
            _verticalVel = 0f;
            _smoothVelRef = Vector3.zero; // clear stale SmoothDamp ref to avoid velocity jerk on landing
        }
    }

    private Vector3 BuildTargetVelocity()
    {
        //convert 2d input into a world space velocity aligned to the camera yaw
        if (_moveInput.sqrMagnitude < 0.01f)
            return Vector3.zero;

        float yaw = mouseLook != null ? mouseLook.GetYaw() : transform.eulerAngles.y;
        Quaternion camYaw = Quaternion.Euler(0f, yaw, 0f);

        Vector3 direction = camYaw * new Vector3(_moveInput.x, 0f, _moveInput.y);
        float speed = _isSprinting ? sprintSpeed : walkSpeed;

        return direction.normalized * speed;
    }

    public void DisableMovement()
    {
        moveAction?.action.Disable();
        jumpAction?.action.Disable();
        sprintAction?.action.Disable();
        if (_agent != null) _agent.enabled = false;
        shooting?.DisableShoot();
    }

    public void EnableMovement()
    {
        moveAction?.action.Enable();
        jumpAction?.action.Enable();
        sprintAction?.action.Enable();
        if (_agent != null) _agent.enabled = true;
        shooting?.EnableShoot();
    }

    public void DisableMouseLook()
    {
        if (mouseLook != null) mouseLook.DisableMouseLook();
    }

    public void EnableMouseLook()
    {
        if (mouseLook != null) mouseLook.EnableMouseLook();
    }
}