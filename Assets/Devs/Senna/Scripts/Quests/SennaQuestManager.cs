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
    [SerializeField] private SennaAmmoSystem ammoSystem;


    [Header("Events")]
    public UnityEvent<SennaQuestData> onQuestCompleted;
    // Fires once when the last main quest completes — the base teleport hooks in here later
    public UnityEvent onAllMainQuestsCompleted;

    // Code-only mirrors of the inspector events
    public event System.Action<SennaQuestData> QuestCompleted;
    public event System.Action AllMainQuestsCompleted;

    // Cached so HUD polling can compare by reference without per-frame string work
    public string ActiveQuestTitle { get; private set; } = "";
    public string ActiveQuestDisplayText { get; private set; } = "";
    public string SideQuestDisplayText { get; private set; } = "";
    public string CurrentPromptText => playerInteractor != null ? playerInteractor.CurrentPromptText : null;
    public string RewardMessage { get; private set; } = "";

    // Quest-complete banner, polled by SennaQuestHUD with the same reference-swap trick as
    // RewardMessage: BannerBody gets a fresh string each completion so the HUD detects the change.
    public string BannerTitle { get; private set; } = "";
    public string BannerBody { get; private set; } = "";

    public bool AllMainQuestsDone => _allMainsDone;

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
                CompleteQuest(q);
        }

        if (!_allMainsDone && _hasMainQuest && ActiveMainQuestIndex() < 0)
        {
            _allMainsDone = true;
            onAllMainQuestsCompleted?.Invoke();
            AllMainQuestsCompleted?.Invoke();
        }

        RebuildDisplayText();
    }

    // Returns true if any quest objective accepted the interaction.
    // SennaQuestInteractable uses this to decide whether to mark itself used.
    public bool ReportInteractionCompleted(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        int activeMain = ActiveMainQuestIndex();
        bool anyAccepted = false;

        for (int q = 0; q < quests.Length; q++)
        {
            if (quests[q] == null || _questDone[q]) continue;
            if (quests[q].isMainQuest && q != activeMain) continue;

            bool changed = false;
            var objectives = quests[q].objectives;
            for (int i = 0; objectives != null && i < objectives.Length; i++)
            {
                var objective = objectives[i];
                if (objective.type != SennaObjectiveType.Interact) continue;
                if (objective.interactKey != key) continue;
                if (_progress[q][i] >= objective.requiredCount) continue;
                _progress[q][i]++;
                changed = true;
                anyAccepted = true;
            }

            if (changed && IsQuestComplete(q))
                CompleteQuest(q);
        }

        if (!_allMainsDone && _hasMainQuest && ActiveMainQuestIndex() < 0)
        {
            _allMainsDone = true;
            onAllMainQuestsCompleted?.Invoke();
            AllMainQuestsCompleted?.Invoke();
        }

        RebuildDisplayText();
        return anyAccepted;
    }

    // Marks quest q complete and fires its reward, completion banner, and events. Shared by both
    // report paths (item collect + interaction) so they can't drift.
    private void CompleteQuest(int q)
    {
        _questDone[q] = true;
        GrantReward(quests[q]);
        SetCompletionBanner(quests[q]);
        onQuestCompleted?.Invoke(quests[q]);
        QuestCompleted?.Invoke(quests[q]);
    }

    // Builds the "QUEST COMPLETE" banner as fresh strings so the HUD's reference-equality poll
    // detects the change. Appends the reward line when GrantReward set one.
    private void SetCompletionBanner(SennaQuestData quest)
    {
        BannerTitle = "QUEST COMPLETE";
        string questName = quest != null && !string.IsNullOrEmpty(quest.questName) ? quest.questName : "Objective complete";
        BannerBody = string.IsNullOrEmpty(RewardMessage) ? questName : questName + "\nYou got: " + RewardMessage;
    }

    private void GrantReward(SennaQuestData quest)
    {
        RewardMessage = "";
        if (quest.rewardPool == null || quest.rewardPool.Length == 0) return;

        var entry = PickReward(quest.rewardPool);
        if (entry == null) return;

        if (entry.ammoAmount > 0)
            ammoSystem?.AddReserve(entry.ammoAmount);

        RewardMessage = entry.displayLabel ?? "";
    }

    private SennaRewardEntry PickReward(SennaRewardEntry[] pool)
    {
        int total = 0;
        foreach (var e in pool) total += Mathf.Max(1, e.weight);

        int roll = Random.Range(0, total);
        int cumulative = 0;
        foreach (var e in pool)
        {
            cumulative += Mathf.Max(1, e.weight);
            if (roll < cumulative) return e;
        }
        return pool[pool.Length - 1];
    }

    // Returns true if picking up this item would count toward any currently active quest.
    // SennaQuestItem uses this to hide its interact prompt until the right quest is active.
    public bool IsItemCollectable(ItemData item)
    {
        if (item == null) return false;
        int activeMain = ActiveMainQuestIndex();

        for (int q = 0; q < quests.Length; q++)
        {
            if (quests[q] == null || _questDone[q]) continue;
            if (quests[q].isMainQuest && q != activeMain) continue;
            var objectives = quests[q].objectives;
            for (int i = 0; objectives != null && i < objectives.Length; i++)
            {
                if (objectives[i].type == SennaObjectiveType.CollectItem && objectives[i].targetItem == item)
                    return true;
            }
        }
        return false;
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
            ActiveQuestTitle = "";
            ActiveQuestDisplayText = _hasMainQuest ? "All objectives complete" : "";
        }
        else
        {
            var quest = quests[active];
            ActiveQuestTitle = quest.questName ?? "";

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

        SideQuestDisplayText = BuildSideQuestText();
    }

    private string BuildSideQuestText()
    {
        var sb = new System.Text.StringBuilder();
        for (int q = 0; q < quests.Length; q++)
        {
            if (quests[q] == null || quests[q].isMainQuest) continue;
            var objectives = quests[q].objectives;
            for (int i = 0; objectives != null && i < objectives.Length; i++)
            {
                var objective = objectives[i];
                if (sb.Length > 0) sb.Append('\n');
                string label = string.IsNullOrEmpty(objective.shortLabel) ? quests[q].questName : objective.shortLabel;
                sb.Append(label).Append(' ').Append(_progress[q][i]).Append('/').Append(objective.requiredCount);
            }
        }
        return sb.ToString();
    }
}
