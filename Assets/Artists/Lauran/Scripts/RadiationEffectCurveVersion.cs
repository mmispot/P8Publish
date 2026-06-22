using UnityEngine;

public class RadiationEffectCurveVersion : MonoBehaviour
{
    [SerializeField] private Material radiationMaterial;
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private RadiationManager radiationManager;
    private static readonly int IntensityIonisation = Shader.PropertyToID("_IntensityIonisation");

    private void Awake()
    {
        radiationManager = FindObjectOfType<RadiationManager>();
    }

    private void Update()
    {
        if (radiationMaterial == null || radiationManager == null) return;

        float curvedValue = intensityCurve.Evaluate(radiationManager.CurrentRadiation);
        radiationMaterial.SetFloat(IntensityIonisation, curvedValue);
    }

    private void OnDestroy()
    {
        if (radiationMaterial != null)
        {
            radiationMaterial.SetFloat(IntensityIonisation, 0f);
        }
    }
}