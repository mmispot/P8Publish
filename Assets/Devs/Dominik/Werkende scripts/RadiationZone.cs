using UnityEngine;

public class RadiationZone : MonoBehaviour
{
    [SerializeField] private float radiationPerSecond = 0.01f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<RadiationManager>(out var manager))
            manager.EnterZone(radiationPerSecond);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<RadiationManager>(out var manager))
            manager.ExitZone(radiationPerSecond);
    }
}