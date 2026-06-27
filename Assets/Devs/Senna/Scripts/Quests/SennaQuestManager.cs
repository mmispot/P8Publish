using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Scene-level quest tracking. Quest assets are read-only; per-quest counters
// live here and reset with the scene.
public class SennaQuestManager : MonoBehaviour
{
    public static SennaQuestManager Instance { get; private set; }

    [Header("Quests")]
    [SerializeField] private SennaQuestData[] quests;
    [SerializeField] private string allQuestsCompleteMessage = "Find the second elevator";

    [Header("References")]
    [SerializeField] private SennaPlayerInteractor playerInteractor;
    [SerializeField] private SennaAmmoSystem ammoSystem;

    [Header("Inventory (for item rewards)")]
    [SerializeField] private GridController gridController;
    [SerializeField] private ItemGrid inventoryGrid;

    [Header("Events")]
    public UnityEvent<SennaQuestData> onQuestCompleted;
    public UnityEvent onAllMainQuestsCompleted;

    public event System.Action<SennaQuestData> QuestCompleted;
    public event System.Action AllMainQuestsCompleted;

    public string ActiveQuestTitle { get; private set; } = "";
    public string ActiveQuestDisplayText { get; private set; } = "";
    public string SideQuestDisplayText { get; private set; } = "";
    public string CurrentPromptText => playerInteractor != null ? playerInteractor.CurrentPromptText : null;
    public string RewardMessage { get; private set; } = "";
    public string BannerTitle { get; private set; } = "";
    public string BannerBody { get; private set; } = "";
    public bool AllMainQuestsDone => _allMainsDone;

    private readonly List<ItemData> _collectedItems = new List<ItemData>();
    public IReadOnlyList<ItemData> CollectedItems => _collectedItems;

    private int[][] _progress;
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
            int count = quests[q] != null && quests[q].objectives != null ? quests[q].objectives.Length : 0;
            _progress[q] = new int[count];
            if (quests[q] != null && quests[q].isMainQuest)
                _hasMainQuest = true;
        }
        RebuildDisplayText();
    }

    // Called by EnemyHealth.Die() for every enemy death.
    public void ReportEnemyKilled()
    {
        int activeMain = ActiveMainQuestIndex();

        for (int q = 0; q < quests.Length; q++)
        {
            if (quests[q] == null || _questDone[q]) continue;
            if (quests[q].isMainQuest && q != activeMain) continue;

            bool changed = false;
            var objectives = quests[q].objectives;
            for (int i = 0; objectives != null && i < objectives.Length; i++)
            {
                var obj = objectives[i];
                if (obj.type != SennaObjectiveType.KillEnemy) continue;
                if (_progress[q][i] >= obj.requiredCount) continue;
                _progress[q][i]++;
                changed = true;
            }

            if (changed && IsQuestComplete(q))
                CompleteQuest(q);
        }

        CheckAllMainsDone();
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
            if (quests[q].isMainQuest && q != activeMain) continue;

            bool changed = false;
            var objectives = quests[q].objectives;
            for (int i = 0; objectives != null && i < objectives.Length; i++)
            {
                var obj = objectives[i];
                if (obj.type != SennaObjectiveType.CollectItem) continue;
                if (obj.targetItem != item) continue;
                if (_progress[q][i] >= obj.requiredCount) continue;
                _progress[q][i]++;
                changed = true;
            }

            if (changed && IsQuestComplete(q))
                CompleteQuest(q);
        }

        CheckAllMainsDone();
        RebuildDisplayText();
    }

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
                var obj = objectives[i];
                if (obj.type != SennaObjectiveType.Interact) continue;
                if (obj.interactKey != key) continue;
                if (_progress[q][i] >= obj.requiredCount) continue;
                _progress[q][i]++;
                changed = true;
                anyAccepted = true;
            }

            if (changed && IsQuestComplete(q))
                CompleteQuest(q);
        }

        CheckAllMainsDone();
        RebuildDisplayText();
        return anyAccepted;
    }

    private void CheckAllMainsDone()
    {
        if (!_allMainsDone && _hasMainQuest && ActiveMainQuestIndex() < 0)
        {
            _allMainsDone = true;
            onAllMainQuestsCompleted?.Invoke();
            AllMainQuestsCompleted?.Invoke();
        }
    }

    private void CompleteQuest(int q)
    {
        _questDone[q] = true;
        GrantReward(quests[q]);
        SetCompletionBanner(quests[q]);
        onQuestCompleted?.Invoke(quests[q]);
        QuestCompleted?.Invoke(quests[q]);
    }

    private void SetCompletionBanner(SennaQuestData quest)
    {
        BannerTitle = "QUEST COMPLETE";
        string questName = quest != null && !string.IsNullOrEmpty(quest.questName) ? quest.questName : "Objective complete";
        BannerBody = string.IsNullOrEmpty(RewardMessage) ? questName : questName + "\nYou got: " + RewardMessage;
    }

    private void GrantReward(SennaQuestData quest)
    {
        var labelParts = new System.Text.StringBuilder();

        // Grant all fixed rewards (main quest guaranteed loot)
        if (quest.fixedRewards != null)
        {
            foreach (var reward in quest.fixedRewards)
            {
                if (reward.item == null) continue;
                InsertItemToInventory(reward.item, reward.quantity);
                if (!string.IsNullOrEmpty(reward.displayLabel))
                {
                    if (labelParts.Length > 0) labelParts.Append(", ");
                    labelParts.Append(reward.displayLabel);
                }
            }
        }

        // Pick one random reward from the pool (side mission loot)
        if (quest.rewardPool != null && quest.rewardPool.Length > 0)
        {
            var entry = PickReward(quest.rewardPool);
            if (entry != null)
            {
                if (entry.ammoAmount > 0)
                    ammoSystem?.AddReserve(entry.ammoAmount);

                if (entry.rewardItem != null)
                    InsertItemToInventory(entry.rewardItem, Mathf.Max(1, entry.quantity));

                if (!string.IsNullOrEmpty(entry.displayLabel))
                {
                    if (labelParts.Length > 0) labelParts.Append(", ");
                    labelParts.Append(entry.displayLabel);
                }
            }
        }

        RewardMessage = labelParts.ToString();
    }

    private void InsertItemToInventory(ItemData item, int quantity)
    {
        if (gridController == null || inventoryGrid == null)
        {
            Debug.LogWarning($"SennaQuestManager: GridController/ItemGrid not assigned — cannot insert {item.name} to inventory.");
            return;
        }

        inventoryGrid.EnsureInitialized();
        var go = Object.Instantiate(gridController.ItemPrefab, gridController.CanvasTransform);
        var invItem = go.GetComponent<InventoryItem>();
        invItem.Set(item);
        invItem.currentStackSize = Mathf.Clamp(quantity, 1, item.stackable ? item.maxStackSize : 1);
        invItem.UpdateStackText();

        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = true;

        gridController.InsertItem(invItem, inventoryGrid);
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
            ActiveQuestDisplayText = _hasMainQuest ? allQuestsCompleteMessage : "";
        }
        else
        {
            var quest = quests[active];
            ActiveQuestTitle = quest.questName ?? "";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; quest.objectives != null && i < quest.objectives.Length; i++)
            {
                var obj = quest.objectives[i];
                if (i > 0) sb.Append('\n');
                string label = string.IsNullOrEmpty(obj.shortLabel) ? quest.questName : obj.shortLabel;
                sb.Append(label).Append(' ').Append(_progress[active][i]).Append('/').Append(obj.requiredCount);
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
                var obj = objectives[i];
                if (sb.Length > 0) sb.Append('\n');
                string label = string.IsNullOrEmpty(obj.shortLabel) ? quests[q].questName : obj.shortLabel;
                sb.Append(label).Append(' ').Append(_progress[q][i]).Append('/').Append(obj.requiredCount);
            }
        }
        return sb.ToString();
    }
}
