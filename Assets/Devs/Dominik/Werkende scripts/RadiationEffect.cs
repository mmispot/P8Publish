using UnityEngine;

public class RadiationEffect : MonoBehaviour
{
    [SerializeField] private Material radiationMaterial;

    private RadiationManager radiationManager;
    private static readonly int IntensityIonisation = Shader.PropertyToID("_IntensityIonisation");

    private void Awake()
    {
        radiationManager = FindObjectOfType<RadiationManager>();
    }

    private void Update()
    {
        if (radiationMaterial == null || radiationManager == null) return;

        radiationMaterial.SetFloat(IntensityIonisation, radiationManager.CurrentRadiation);
    }

    private void OnDestroy()
    {
        if (radiationMaterial != null)
        {
            radiationMaterial.SetFloat(IntensityIonisation, 0f);
        }
    }
}