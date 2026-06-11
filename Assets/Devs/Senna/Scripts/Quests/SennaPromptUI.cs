using TMPro;
using UnityEngine;

// Center-screen pickup/interaction prompt. Polls like SennaQuestHUD;
// prompt strings are cached by their targets, so the reference compare
// only fires when the targeted object actually changes.
public class SennaPromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;

    private string _last;

    private void Update()
    {
        string current = SennaQuestManager.Instance != null
            ? SennaQuestManager.Instance.CurrentPromptText
            : null;

        if (ReferenceEquals(current, _last)) return;
        _last = current;

        if (promptText == null) return;
        promptText.text = current ?? "";
        promptText.enabled = !string.IsNullOrEmpty(current);
    }
}
