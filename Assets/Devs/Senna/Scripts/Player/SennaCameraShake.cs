using UnityEngine;

[DefaultExecutionOrder(100)]
public class SennaCameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeDecay = 4f;
    [SerializeField] private float positionMagnitude = 0.05f;
    [SerializeField] private float rotationMagnitude = 2.8f;

    private float _trauma;
    private float _noiseOffset;

    private void Awake()
    {
        _noiseOffset = Random.Range(0f, 100f);
    }

    private void LateUpdate()
    {
        if (_trauma <= 0f) return;

        _trauma = Mathf.Max(0f, _trauma - shakeDecay * Time.deltaTime);
        float intensity = _trauma * _trauma;

        float t = Time.time + _noiseOffset;
        transform.localPosition += new Vector3(
            (Mathf.PerlinNoise(t * 3f, 0f) - 0.5f) * 2f * positionMagnitude * intensity,
            (Mathf.PerlinNoise(0f, t * 3f) - 0.5f) * 2f * positionMagnitude * intensity,
            0f);

        transform.localRotation *= Quaternion.Euler(
            (Mathf.PerlinNoise(t * 2.5f, 100f) - 0.5f) * 2f * rotationMagnitude * intensity,
            (Mathf.PerlinNoise(200f, t * 2.5f) - 0.5f) * 2f * rotationMagnitude * intensity,
            0f);
    }

    public void TriggerShake(float traumaAmount)
    {
        _trauma = Mathf.Min(1f, _trauma + traumaAmount);
    }
}
