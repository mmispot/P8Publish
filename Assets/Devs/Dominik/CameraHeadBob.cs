using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    [Header("References")]
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

    private Vector3 _startPos;
    private float _bobTimer;
    private float _currentAmplitude;
    private float _currentFrequency;
    private float _landingBump;
    private bool _wasGrounded;

    private void Start()
    {
        //store the initial local position as the rest position
        _startPos = transform.localPosition;
        _wasGrounded = playerMovement != null && playerMovement.IsGrounded;
    }

    private void Update()
    {
        //do nothing if there is no player movement reference
        if (playerMovement == null) return;

        bool grounded = playerMovement.IsGrounded;
        bool moving = playerMovement.IsMoving;
        bool sprinting = playerMovement.IsSprinting;

        HandleLandingBump(grounded);

        if (grounded && moving)
            ApplyBob(sprinting);
        else
            ReturnToRest();
    }

    private void HandleLandingBump(bool grounded)
    {
        //trigger a downward bump when the player lands
        if (grounded && !_wasGrounded)
            _landingBump = -landingAmplitude;

        _wasGrounded = grounded;
        _landingBump = Mathf.Lerp(_landingBump, 0f, Time.deltaTime * landingDecaySpeed);
    }

    private void ApplyBob(bool sprinting)
    {
        //lerp amplitude and frequency towards walk or run targets
        float targetAmplitude = sprinting ? runAmplitude : walkAmplitude;
        float targetFrequency = sprinting ? runFrequency : walkFrequency;

        _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmplitude, Time.deltaTime * bobTransitionSpeed);
        _currentFrequency = Mathf.Lerp(_currentFrequency, targetFrequency, Time.deltaTime * bobTransitionSpeed);

        _bobTimer += Time.deltaTime * _currentFrequency;

        //figure-8 pattern: Y bobs at full frequency, X sways at half
        float bobY = Mathf.Sin(_bobTimer * Mathf.PI * 2f) * _currentAmplitude;
        float bobX = Mathf.Sin(_bobTimer * Mathf.PI) * _currentAmplitude * 0.5f;

        transform.localPosition = new Vector3(
            _startPos.x + bobX,
            _startPos.y + bobY + _landingBump,
            _startPos.z);
    }

    private void ReturnToRest()
    {
        //smoothly decay amplitude and frequency back to zero
        _currentAmplitude = Mathf.Lerp(_currentAmplitude, 0f, Time.deltaTime * bobTransitionSpeed);
        _currentFrequency = Mathf.Lerp(_currentFrequency, 0f, Time.deltaTime * bobTransitionSpeed);

        //lerp position back to rest, keeping any active landing bump
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            new Vector3(_startPos.x, _startPos.y + _landingBump, _startPos.z),
            Time.deltaTime * returnSpeed);
    }
}