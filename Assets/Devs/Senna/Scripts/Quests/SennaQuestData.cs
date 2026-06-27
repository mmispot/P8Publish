using UnityEngine;

// Quest definition asset. Runtime progress lives in SennaQuestManager —
// these assets are never mutated in play mode.
[CreateAssetMenu(menuName = "Senna/Quest")]
public class SennaQuestData : ScriptableObject
{
    public string questName;
    [TextArea] public string description;
    public bool isMainQuest;

    public SennaQuestObjective[] objectives;

    [Header("Fixed Rewards (all granted on completion)")]
    public SennaFixedReward[] fixedRewards;

    [Header("Random Reward Pool (one entry picked on completion)")]
    public SennaRewardEntry[] rewardPool;
}

[System.Serializable]
public class SennaFixedReward
{
    public ItemData item;
    public int quantity = 1;
    public string displayLabel;
}

[System.Serializable]
public class SennaRewardEntry
{
    public string displayLabel;
    public int ammoAmount;
    public ItemData rewardItem;
    public int quantity = 1;
    [Range(1, 100)] public int weight = 10;
}

[System.Serializable]
public class SennaQuestObjective
{
    public SennaObjectiveType type;
    public ItemData targetItem;
    public string interactKey;
    public int requiredCount = 1;
    public string shortLabel;
}

public enum SennaObjectiveType
{
    CollectItem,
    Interact,
    KillEnemy
}
