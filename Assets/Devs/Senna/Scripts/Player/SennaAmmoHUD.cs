using TMPro;
using UnityEngine;

// Bottom-right ammo readout: "mag / reserve" (e.g. "12 / 60"). Polls SennaAmmoSystem and only
// rewrites the text when a count actually changes — same poll + init-order reasoning as
// SennaHealthBarUI / SennaQuestHUD (the player starts inactive until Start is pressed, so polling
// stays correct regardless of init order). The number turns red when the magazine is empty.
public class SennaAmmoHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SennaAmmoSystem ammo;
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.95f, 0.93f, 0.88f, 1f);
    [SerializeField] private Color emptyColor  = new Color(0.85f, 0.15f, 0.15f, 1f);

    private int _lastMag = -1;
    private int _lastReserve = -1;

    private void OnEnable()
    {
        // Late-bind if the reference wasn't set in the inspector (e.g. the player activated after
        // this HUD). One-off lookup on enable only — never per frame.
        if (ammo == null)
            ammo = Object.FindFirstObjectByType<SennaAmmoSystem>(FindObjectsInactive.Include);

        _lastMag = _lastReserve = -1; // force a redraw on the next Update
    }

    private void Update()
    {
        if (ammo == null || ammoText == null) return;

        int mag = ammo.CurrentInMag;
        int reserve = ammo.ReserveAmmo;
        if (mag == _lastMag && reserve == _lastReserve) return; // only touch the text on a real change

        _lastMag = mag;
        _lastReserve = reserve;
        ammoText.text = $"{mag} / {reserve}";
        ammoText.color = mag > 0 ? normalColor : emptyColor;
    }
}
