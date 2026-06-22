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

    [Header("Reward Pool")]
    [Tooltip("One entry is picked at random on quest completion.")]
    public SennaRewardEntry[] rewardPool;
}

[System.Serializable]
public class SennaRewardEntry
{
    public string displayLabel;   // shown in the banner, e.g. "Ammo x20" or "Scrap Metal x3"
    public int ammoAmount;        // > 0 grants ammo to reserve via SennaAmmoSystem
    [Range(1, 100)] public int weight = 10;
}

[System.Serializable]
public class SennaQuestObjective
{
    public SennaObjectiveType type;

    // CollectItem: which ItemData counts toward this objective
    public ItemData targetItem;

    // Interact: string key that SennaQuestInteractable reports
    public string interactKey;

    public int requiredCount = 1;

    // HUD label, e.g. "Find power cells" — falls back to the quest name when empty
    public string shortLabel;
}

public enum SennaObjectiveType
{
    CollectItem,
    Interact
}
