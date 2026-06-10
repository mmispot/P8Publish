using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(TrailRenderer))]
public class SennaBulletTracer : MonoBehaviour
{
    [SerializeField] private float speed = 150f;
    [SerializeField] private float maxLifetime = 2f;

    [Header("Trail Look (applied in Awake)")]
    [SerializeField] private float trailTime = 0.06f;
    [SerializeField] private float startWidth = 0.02f;
    [SerializeField] private float endWidth = 0.005f;
    [SerializeField] private Color startColor = new Color(1f, 0.95f, 0.6f, 1f);
    [SerializeField] private Color endColor = new Color(1f, 0.5f, 0.1f, 0f);

    private TrailRenderer _trail;
    private Vector3 _endPoint;
    private System.Action<SennaBulletTracer> _returnToPool;
    private bool _traveling;
    private float _lifetime;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        ConfigureTrail();
    }

    // Self-configuring so the prefab needs no manual material/width setup.
    // Sprites/Default renders vertex colors and works in URP — no magenta.
    private void ConfigureTrail()
    {
        _trail.time = trailTime;
        _trail.startWidth = startWidth;
        _trail.endWidth = endWidth;
        _trail.minVertexDistance = 0.01f;
        _trail.shadowCastingMode = ShadowCastingMode.Off;
        _trail.receiveShadows = false;
        _trail.alignment = LineAlignment.View;
        _trail.startColor = startColor;
        _trail.endColor = endColor;

        if (_trail.sharedMaterial == null || _trail.sharedMaterial.shader == null ||
            _trail.sharedMaterial.shader.name == "Hidden/InternalErrorShader" ||
            _trail.sharedMaterial.name.StartsWith("Default-Line"))
        {
            _trail.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    public void Initialize(Vector3 start, Vector3 end, System.Action<SennaBulletTracer> returnToPool)
    {
        _endPoint = end;
        _returnToPool = returnToPool;
        _lifetime = 0f;
        _traveling = true;

        // Teleport before clearing so no streak is drawn from the pooled position
        transform.position = start;
        _trail.Clear();
        _trail.emitting = true;

        Vector3 dir = end - start;
        if (dir.sqrMagnitude > 0.001f)
            transform.forward = dir.normalized;
    }

    private void Update()
    {
        if (!_traveling) return;

        _lifetime += Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _endPoint, speed * Time.deltaTime);

        if ((transform.position - _endPoint).sqrMagnitude < 0.0001f || _lifetime >= maxLifetime)
            ReturnToPool();
    }

    private void ReturnToPool()
    {
        _traveling = false;
        _trail.emitting = false;
        _trail.Clear();
        _returnToPool?.Invoke(this);
    }
}
