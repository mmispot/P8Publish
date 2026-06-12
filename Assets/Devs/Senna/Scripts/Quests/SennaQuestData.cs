using UnityEngine;

// Quest definition asset. Runtime progress lives in SennaQuestManager —
// these assets are never mutated in play mode.
[CreateAssetMenu(menuName = "Senna/Quest")]
public class SennaQuestData : ScriptableObject
{
    public string questName;
    [TextArea] public string description;

    // Main quests show in the HUD and advance in SennaQuestManager's array order.
    // Side quests progress silently and exist for crafting rewards.
    public bool isMainQuest;

    public SennaQuestObjective[] objectives;

    // Crafting items handed out on completion (granting comes with the inventory integration).
    public ItemData[] rewardItems;
}

[System.Serializable]
public class SennaQuestObjective
{
    public SennaObjectiveType type;

    // CollectItem: which ItemData counts toward this objective
    public ItemData targetItem;
    public int requiredCount = 1;

    // HUD label, e.g. "Find power cells" — falls back to the quest name when empty
    public string shortLabel;
}

public enum SennaObjectiveType
{
    CollectItem
    // Kill / Interact objective types slot in here later
}
