using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Beweging")]
    public float moveSpeed = 5f;
    public float smoothTime = 0.05f;

    [Header("Sprint")]
    public float sprintSpeed = 9f;
    public InputActionReference sprintAction;

    [Header("Springen")]
    public float jumpForce = 5f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Input Actions")]
    public InputActionReference movementAction;
    public InputActionReference jumpAction;

    [Header("Referenties")]
    public MouseLook mouseLook;

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 smoothVelocity;
    private bool jumpRequested = false;
    private bool isSprinting = false;

    public bool IsSprinting => isSprinting;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;
    public bool IsGrounded() => groundCheck == null || Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;
    }

    private void OnEnable()
    {
        movementAction?.action.Enable();
        jumpAction?.action.Enable();
        sprintAction?.action.Enable();

        if (jumpAction?.action != null)
            jumpAction.action.performed += OnJump;
    }

    private void OnDisable()
    {
        movementAction?.action.Disable();
        jumpAction?.action.Disable();
        sprintAction?.action.Disable();

        if (jumpAction?.action != null)
            jumpAction.action.performed -= OnJump;
    }

    //checks if grounded then jump otherwise it would be infinte jump
    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (IsGrounded())
            jumpRequested = true;
        Debug.Log("Player Grounded");
    }

    private void Update()
    {
        if (movementAction?.action != null)
            moveInput = movementAction.action.ReadValue<Vector2>();

        isSprinting = sprintAction?.action.IsPressed() ?? false;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Gebruik camera yaw als vooruit-richting, body roteert niet zelf
        float yaw = mouseLook != null ? mouseLook.GetYaw() : transform.eulerAngles.y;

        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        Vector3 targetDir = right * moveInput.x + forward * moveInput.y;
        if (targetDir.sqrMagnitude > 1f) targetDir.Normalize();

        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 targetHVel = targetDir * currentSpeed;

        // Vloeiend naar doelsnelheid
        Vector3 currentHVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 smoothHVel = Vector3.SmoothDamp(
            currentHVel, targetHVel, ref smoothVelocity,
            smoothTime, Mathf.Infinity, Time.fixedDeltaTime);

        // Positie updaten, verticale physics behouden
        Vector3 hDisplacement = smoothHVel * Time.fixedDeltaTime;
        Vector3 vDisplacement = Vector3.up * rb.linearVelocity.y * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + hDisplacement + vDisplacement);

        // Springen
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
        }
    }

    //check testing for groundcheck
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    //for stop walking
    public void DisableMovement()
    {
        movementAction?.action.Disable();
        jumpAction?.action.Disable();
        sprintAction?.action.Disable();
    }
    //for start walking
    public void EnableMovement()
    {
        movementAction?.action.Enable();
        jumpAction?.action.Enable();
        sprintAction?.action.Enable();
    }
}
