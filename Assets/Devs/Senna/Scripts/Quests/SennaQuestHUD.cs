using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top-left quest panel: quest title, main objectives, and side quests.
// Polls the manager instead of subscribing — same init-order reasoning as
// SennaHealthBarUI. The manager caches its display strings, so the reference
// compares keep this allocation-free per frame. Empty rows are deactivated so
// the panel's layout shrinks around them; the background hides when the whole
// panel is empty (e.g. a scene without a SennaQuestManager).
public class SennaQuestHUD : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI questText;
    [SerializeField] private GameObject sideHeader;
    [SerializeField] private TextMeshProUGUI sideQuestText;

    private string _lastTitle;
    private string _lastMain;
    private string _lastSide;

    private void Update()
    {
        var manager = SennaQuestManager.Instance;

        UpdateRow(titleText, manager != null ? manager.ActiveQuestTitle : null, ref _lastTitle);
        UpdateRow(questText, manager != null ? manager.ActiveQuestDisplayText : null, ref _lastMain);
        bool sideChanged = UpdateRow(sideQuestText, manager != null ? manager.SideQuestDisplayText : null, ref _lastSide);

        if (sideChanged && sideHeader != null && sideQuestText != null)
            sideHeader.SetActive(sideQuestText.gameObject.activeSelf);

        if (background != null)
        {
            bool anyRow = (titleText != null && titleText.gameObject.activeSelf)
                       || (questText != null && questText.gameObject.activeSelf)
                       || (sideQuestText != null && sideQuestText.gameObject.activeSelf);
            if (background.enabled != anyRow)
                background.enabled = anyRow;
        }
    }

    private bool UpdateRow(TextMeshProUGUI row, string value, ref string last)
    {
        if (ReferenceEquals(value, last)) return false;
        last = value;
        if (row == null) return false;

        row.text = value ?? "";
        row.gameObject.SetActive(!string.IsNullOrEmpty(value));
        return true;
    }
}
