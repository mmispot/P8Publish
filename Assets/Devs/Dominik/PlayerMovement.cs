using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
    public class PlayerMovement : MonoBehaviour
{
    public Rigidbody rb;
    public Transform cameraTransform;      // assign your camera (used for yaw)
    public float moveSpeed = 5f;
    public InputActionReference movement;

    private Vector2 _moveInput;
    private Vector3 _smoothVelocity;

    [Tooltip("Smoothing time for horizontal velocity.")]
    public float smoothTime = 0.05f;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void OnEnable()
    {
        if (movement?.action != null) movement.action.Enable();
    }

    private void OnDisable()
    {
        if (movement?.action != null) movement.action.Disable();
    }

    private void Update()
    {
        if (movement?.action != null)
            _moveInput = movement.action.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Compute yaw-only basis so pitch doesn't affect movement
        float yaw = (cameraTransform != null) ? cameraTransform.eulerAngles.y : transform.eulerAngles.y;
        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        // Desired horizontal velocity
        Vector3 targetDir = (right * _moveInput.x + forward * _moveInput.y);
        if (targetDir.sqrMagnitude > 1f) targetDir.Normalize();
        Vector3 targetHorizontalVel = targetDir * moveSpeed;

        // Smooth horizontal velocity (preserve vertical velocity from physics)
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 smoothHorizontalVel = Vector3.SmoothDamp(currentHorizontalVel, targetHorizontalVel, ref _smoothVelocity, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);

        // Move using MovePosition to avoid jitter from directly setting velocity
        Vector3 displacement = smoothHorizontalVel * Time.fixedDeltaTime;
        Vector3 verticalDisplacement = Vector3.up * rb.linearVelocity.y * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + displacement + verticalDisplacement);
    }
}