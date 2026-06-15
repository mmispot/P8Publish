using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Center-screen pickup/interaction prompt on a dark auto-sized pill.
// Polls like SennaQuestHUD; prompt strings are cached by their targets,
// so the reference compare only fires when the targeted object changes.
public class SennaPromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Image background; // pill behind the text, shown/hidden with it

    private string _last;

    private void Update()
    {
        string current = SennaQuestManager.Instance != null
            ? SennaQuestManager.Instance.CurrentPromptText
            : null;

        if (ReferenceEquals(current, _last)) return;
        _last = current;

        bool show = !string.IsNullOrEmpty(current);
        if (promptText != null)
        {
            promptText.text = current ?? "";
            promptText.enabled = show;
        }
        if (background != null)
            background.enabled = show;
    }
}
