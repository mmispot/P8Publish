using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    [Header("Referenties")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Walk Bob")]
    [SerializeField] private float walkAmplitude = 0.03f;
    [SerializeField] private float walkFrequency = 1.6f;

    [Header("Run Bob")]
    [SerializeField] private float runAmplitude = 0.07f;
    [SerializeField] private float runFrequency = 2.6f;

    [Header("Landing Impact")]
    [SerializeField] private float landingAmplitude = 0.08f;
    [SerializeField] private float landingDecaySpeed = 10f;

    [Header("Smoothing")]
    [SerializeField] private float bobTransitionSpeed = 8f;
    [SerializeField] private float returnSpeed = 10f;

    private Vector3 startPos;
    private float bobTimer;
    private float currentAmplitude;
    private float currentFrequency;
    private float landingBump;
    private bool wasGrounded;

    private void Start()
    {
        startPos = transform.localPosition;
        wasGrounded = playerMovement != null && playerMovement.IsGrounded();
    }

    private void Update()
    {
        if (playerMovement == null) return;

        bool grounded  = playerMovement.IsGrounded();
        bool moving    = playerMovement.IsMoving;
        bool sprinting = playerMovement.IsSprinting;

        // Camera dips down on landing
        if (grounded && !wasGrounded)
            landingBump = -landingAmplitude;
        wasGrounded = grounded;

        landingBump = Mathf.Lerp(landingBump, 0f, Time.deltaTime * landingDecaySpeed);

        if (grounded && moving)
        {
            float targetAmplitude = sprinting ? runAmplitude : walkAmplitude;
            float targetFrequency = sprinting ? runFrequency : walkFrequency;

            currentAmplitude = Mathf.Lerp(currentAmplitude, targetAmplitude, Time.deltaTime * bobTransitionSpeed);
            currentFrequency = Mathf.Lerp(currentFrequency, targetFrequency, Time.deltaTime * bobTransitionSpeed);

            bobTimer += Time.deltaTime * currentFrequency;

            // Figure-8: Y at full frequency, X at half (one sway per two steps)
            float bobY = Mathf.Sin(bobTimer * Mathf.PI * 2f) * currentAmplitude;
            float bobX = Mathf.Sin(bobTimer * Mathf.PI) * currentAmplitude * 0.5f;

            transform.localPosition = new Vector3(
                startPos.x + bobX,
                startPos.y + bobY + landingBump,
                startPos.z);
        }
        else
        {
            currentAmplitude = Mathf.Lerp(currentAmplitude, 0f, Time.deltaTime * bobTransitionSpeed);
            currentFrequency = Mathf.Lerp(currentFrequency, 0f, Time.deltaTime * bobTransitionSpeed);

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                new Vector3(startPos.x, startPos.y + landingBump, startPos.z),
                Time.deltaTime * returnSpeed);
        }
    }
}
