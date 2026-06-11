using TMPro;
using UnityEngine;

// Top-left active quest text. Polls the manager instead of subscribing —
// same init-order reasoning as SennaHealthBarUI: the manager/player may
// activate after this UI is already enabled. The manager caches the display
// string, so the reference compare makes this allocation-free per frame.
public class SennaQuestHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questText;

    private string _last;

    private void Update()
    {
        string current = SennaQuestManager.Instance != null
            ? SennaQuestManager.Instance.ActiveQuestDisplayText
            : null;

        if (ReferenceEquals(current, _last)) return;
        _last = current;

        if (questText != null)
            questText.text = current ?? "";
    }
}
