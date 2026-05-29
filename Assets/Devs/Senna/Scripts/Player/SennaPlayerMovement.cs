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

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float verticalClamp = 85f;

    [Header("Head Bob")]
    [SerializeField] private float bobFrequency = 2.4f;
    [SerializeField] private float bobAmplitude = 0.04f;

    [Header("Camera Effects")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float normalFov = 75f;
    [SerializeField] private float sprintFov = 85f;
    [SerializeField] private float strafeTiltAngle = 1.5f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference lookAction;

    private NavMeshAgent _agent;
    private Vector3 _smoothVelocity;
    private float _verticalRotation;
    private float _cameraRoll;
    private float _bobTimer;
    private Vector3 _cameraRestPos;
    private bool _movementEnabled = true;
    private bool _mouseLookEnabled = true;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.angularSpeed = 0f;

        if (playerCamera == null && cameraTransform != null)
            playerCamera = cameraTransform.GetComponent<Camera>();

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
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        sprintAction.action.Disable();
        lookAction.action.Disable();
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseLook();
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
        _verticalRotation = Mathf.Clamp(_verticalRotation - look.y, -verticalClamp, verticalClamp);
        transform.Rotate(Vector3.up * look.x);
    }

    private void HandleMovement()
    {
        Vector2 input = _movementEnabled ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        bool sprinting = _movementEnabled && sprintAction.action.IsPressed();
        float targetSpeed = sprinting ? sprintSpeed : walkSpeed;

        Vector3 target = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
        float rate = input.magnitude > 0.01f ? acceleration : deceleration;
        _smoothVelocity = Vector3.Lerp(_smoothVelocity, target, rate * Time.deltaTime);

        // Keep agent in sync with transform, then apply our movement through NavMesh
        _agent.nextPosition = transform.position;
        _agent.Move(_smoothVelocity * Time.deltaTime);
        transform.position = _agent.nextPosition;
    }

    private void HandleHeadBob()
    {
        if (cameraTransform == null) return;

        float speed = new Vector3(_smoothVelocity.x, 0f, _smoothVelocity.z).magnitude;

        if (speed < 0.3f)
        {
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition, _cameraRestPos, 8f * Time.deltaTime);
            _bobTimer = 0f;
            return;
        }

        _bobTimer += Time.deltaTime * bobFrequency * (speed / walkSpeed);

        Vector3 bob = new Vector3(
            Mathf.Sin(_bobTimer * 0.5f) * bobAmplitude * 0.5f,
            Mathf.Abs(Mathf.Sin(_bobTimer)) * bobAmplitude,
            0f);

        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition, _cameraRestPos + bob, 12f * Time.deltaTime);
    }

    private void HandleCameraEffects()
    {
        // Sprint FOV kick
        if (playerCamera != null)
        {
            bool sprinting = _movementEnabled && sprintAction.action.IsPressed() && _smoothVelocity.magnitude > 1f;
            float targetFov = sprinting ? sprintFov : normalFov;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, 8f * Time.deltaTime);
        }

        // Strafe camera tilt
        Vector2 input = _movementEnabled ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        _cameraRoll = Mathf.Lerp(_cameraRoll, -input.x * strafeTiltAngle, 8f * Time.deltaTime);
    }

    public void EnableMovement() => _movementEnabled = true;
    public void DisableMovement() { _movementEnabled = false; _smoothVelocity = Vector3.zero; }
    public void EnableMouseLook() => _mouseLookEnabled = true;
    public void DisableMouseLook() => _mouseLookEnabled = false;
}
