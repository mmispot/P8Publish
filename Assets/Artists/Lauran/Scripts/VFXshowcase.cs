using UnityEngine;
using UnityEngine.VFX;

public class VFXShowcase : MonoBehaviour
{
    [SerializeField] private VisualEffect bloodBurst;
    [SerializeField] private VisualEffect takorevBurst;

    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float delayBetween = 0.1f;

    void Start()
    {
        if (bloodBurst != null)
            Invoke(nameof(PlayBloodBurst), startDelay);

        if (takorevBurst != null)
            Invoke(nameof(PlayTakorev), startDelay + delayBetween);
    }

    void PlayBloodBurst()
    {
        bloodBurst.Play();
    }

    void PlayTakorev()
    {
        takorevBurst.Play();
    }
}