using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Scene-level quest tracking. Quest assets are read-only; per-quest counters
// live here and reset with the scene. The HUD polls ActiveQuestDisplayText and
// CurrentPromptText (same init-order reasoning as SennaHealthBarUI).
public class SennaQuestManager : MonoBehaviour
{
    public static SennaQuestManager Instance { get; private set; }

    [Header("Quests")]
    [SerializeField] private SennaQuestData[] quests; // main quests advance in array order

    [Header("References")]
    [SerializeField] private SennaPlayerInteractor playerInteractor;

    [Header("Events")]
    public UnityEvent<SennaQuestData> onQuestCompleted;
    // Fires once when the last main quest completes — the base teleport hooks in here later
    public UnityEvent onAllMainQuestsCompleted;

    // Code-only mirrors of the inspector events
    public event System.Action<SennaQuestData> QuestCompleted;
    public event System.Action AllMainQuestsCompleted;

    // Cached so HUD polling can compare by reference without per-frame string work
    public string ActiveQuestDisplayText { get; private set; } = "";
    public string CurrentPromptText => playerInteractor != null ? playerInteractor.CurrentPromptText : null;

    private readonly List<ItemData> _collectedItems = new List<ItemData>();
    public IReadOnlyList<ItemData> CollectedItems => _collectedItems; // for later inventory insertion

    private int[][] _progress; // [quest][objective]
    private bool[] _questDone;
    private bool _hasMainQuest;
    private bool _allMainsDone;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (quests == null) quests = new SennaQuestData[0];
        _progress = new int[quests.Length][];
        _questDone = new bool[quests.Length];
        for (int q = 0; q < quests.Length; q++)
        {
            int objectiveCount = quests[q] != null && quests[q].objectives != null ? quests[q].objectives.Length : 0;
            _progress[q] = new int[objectiveCount];
            if (quests[q] != null && quests[q].isMainQuest)
                _hasMainQuest = true;
        }

        RebuildDisplayText();
    }

    public void ReportItemCollected(ItemData item)
    {
        if (item != null)
            _collectedItems.Add(item);

        int activeMain = ActiveMainQuestIndex();

        for (int q = 0; q < quests.Length; q++)
        {
            if (quests[q] == null || _questDone[q]) continue;
            // Only the active main quest counts; side quests all progress in parallel
            if (quests[q].isMainQuest && q != activeMain) continue;

            bool changed = false;
            var objectives = quests[q].objectives;
            for (int i = 0; objectives != null && i < objectives.Length; i++)
            {
                var objective = objectives[i];
                if (objective.type != SennaObjectiveType.CollectItem) continue;
                if (objective.targetItem != item) continue;
                if (_progress[q][i] >= objective.requiredCount) continue;
                _progress[q][i]++;
                changed = true;
            }

            if (changed && IsQuestComplete(q))
            {
                _questDone[q] = true;
                onQuestCompleted?.Invoke(quests[q]);
                QuestCompleted?.Invoke(quests[q]);
            }
        }

        if (!_allMainsDone && _hasMainQuest && ActiveMainQuestIndex() < 0)
        {
            _allMainsDone = true;
            onAllMainQuestsCompleted?.Invoke();
            AllMainQuestsCompleted?.Invoke();
        }

        RebuildDisplayText();
    }

    private int ActiveMainQuestIndex()
    {
        for (int q = 0; q < quests.Length; q++)
            if (quests[q] != null && quests[q].isMainQuest && !_questDone[q])
                return q;
        return -1;
    }

    private bool IsQuestComplete(int q)
    {
        var objectives = quests[q].objectives;
        for (int i = 0; objectives != null && i < objectives.Length; i++)
            if (_progress[q][i] < objectives[i].requiredCount)
                return false;
        return true;
    }

    private void RebuildDisplayText()
    {
        int active = ActiveMainQuestIndex();
        if (active < 0)
        {
            ActiveQuestDisplayText = _hasMainQuest ? "All objectives complete" : "";
            return;
        }

        var quest = quests[active];
        var sb = new System.Text.StringBuilder();
        for (int i = 0; quest.objectives != null && i < quest.objectives.Length; i++)
        {
            var objective = quest.objectives[i];
            if (i > 0) sb.Append('\n');
            string label = string.IsNullOrEmpty(objective.shortLabel) ? quest.questName : objective.shortLabel;
            sb.Append(label).Append(' ').Append(_progress[active][i]).Append('/').Append(objective.requiredCount);
        }
        ActiveQuestDisplayText = sb.ToString();
    }
}
