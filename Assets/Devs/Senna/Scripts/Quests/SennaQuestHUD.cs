using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top-left quest panel: quest title, main objectives, and side quests.
// Polls the manager every frame — same pattern as SennaHealthBarUI.
// Reference compares on cached strings keep it allocation-free.
public class SennaQuestHUD : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI questText;
    [SerializeField] private GameObject sideHeader;
    [SerializeField] private TextMeshProUGUI sideQuestText;

    [Header("Reward Banner")]
    [SerializeField] private GameObject rewardBannerRoot;
    [SerializeField] private TextMeshProUGUI rewardBannerTitle;
    [SerializeField] private TextMeshProUGUI rewardBanner;
    [SerializeField] private float bannerDuration = 1.5f;

    private string _lastTitle;
    private string _lastMain;
    private string _lastSide;
    private string _lastReward;
    private bool _wasAllDone;
    private Coroutine _bannerRoutine;

    private static readonly Color ColorObjective  = new Color(0.95f, 0.93f, 0.88f, 1f);
    private static readonly Color ColorAllDone    = new Color(1f,    0.82f, 0.25f, 1f);

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

        // Turn the objective text gold when all quests are done
        bool allDone = manager != null && manager.AllMainQuestsDone;
        if (allDone != _wasAllDone)
        {
            _wasAllDone = allDone;
            if (questText != null)
                questText.color = allDone ? ColorAllDone : ColorObjective;
        }

        // Reward banner: fire when RewardMessage changes to a new non-empty string.
        // Using the same reference-equality trick as the text rows above — the manager
        // creates a new string object each time a reward is granted, so the reference
        // changes even if the text content happens to be the same two quests in a row.
        string reward = manager != null ? manager.RewardMessage : null;
        if (!ReferenceEquals(reward, _lastReward))
        {
            _lastReward = reward;
            if (!string.IsNullOrEmpty(reward) && rewardBannerRoot != null)
            {
                if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
                _bannerRoutine = StartCoroutine(ShowBanner(reward));
            }
        }
    }

    private IEnumerator ShowBanner(string rewardLine)
    {
        if (rewardBannerTitle != null) rewardBannerTitle.text = "YOU GOT";
        if (rewardBanner != null) rewardBanner.text = rewardLine;

        var rt = rewardBannerRoot.GetComponent<RectTransform>();
        rt.localScale = Vector3.zero;
        rewardBannerRoot.SetActive(true);

        yield return ScaleTo(rt, 0f, 1.12f, 0.15f);   // punch in
        yield return ScaleTo(rt, 1.12f, 1.0f, 0.08f);  // settle

        yield return new WaitForSeconds(bannerDuration);

        yield return ScaleTo(rt, 1.0f, 0f, 0.1f);      // pop out
        rewardBannerRoot.SetActive(false);
        rt.localScale = Vector3.one;
        _bannerRoutine = null;
    }

    private IEnumerator ScaleTo(RectTransform rt, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            rt.localScale = Vector3.one * Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        rt.localScale = Vector3.one * to;
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
