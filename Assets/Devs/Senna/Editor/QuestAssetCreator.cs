#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class QuestAssetCreator
{
    private const string QuestFolder   = "Assets/Devs/Senna/Quests";
    private const string LauranPrefabs = "Assets/Artists/Lauran/Prefabs";

    // ── Create all ScriptableObject assets ──────────────────────────────────

    [MenuItem("Tools/Senna/Create Quest Assets")]
    public static void CreateQuestAssets()
    {
        var note   = GetOrCreateItemData("Item_ResearchersNote", 1, 2, false, 1,  ItemData.ItemType.Other);
        var bottle = GetOrCreateItemData("Item_GlassBottle",     1, 1, true,  5,  ItemData.ItemType.Other);
        var meat   = GetOrCreateItemData("Item_MutantMeat",      1, 1, true,  10, ItemData.ItemType.Other);
        var med    = GetOrCreateItemData("Item_Medicine",        1, 1, true,  5,  ItemData.ItemType.Other);
        var scrap  = AssetDatabase.LoadAssetAtPath<ItemData>($"{QuestFolder}/Item_ScrapMetal.asset")
                  ?? GetOrCreateItemData("Item_Scrap",           1, 1, true,  20, ItemData.ItemType.Other);

        // Q1 — Investigate the Lab
        var q1 = ScriptableObject.CreateInstance<SennaQuestData>();
        q1.questName   = "Investigate the Lab";
        q1.description = "Something went wrong in the research lab. Eliminate the hostiles and find a way to arm yourself.";
        q1.isMainQuest = true;
        q1.objectives  = new SennaQuestObjective[]
        {
            new SennaQuestObjective { type = SennaObjectiveType.KillEnemy, requiredCount = 2, shortLabel = "Kill hostiles" },
            new SennaQuestObjective { type = SennaObjectiveType.Interact,  interactKey = "find_pistol", requiredCount = 1, shortLabel = "Find a weapon" }
        };
        q1.fixedRewards = new SennaFixedReward[]
        {
            new SennaFixedReward { item = note,   quantity = 1, displayLabel = "Researcher's Note x1" },
            new SennaFixedReward { item = bottle, quantity = 3, displayLabel = "Glass Bottle x3" }
        };
        SaveIfNew(q1, "Q1_InvestigateLab");

        // Q2 — Find the Elevator
        var q2 = ScriptableObject.CreateInstance<SennaQuestData>();
        q2.questName   = "Find the Elevator";
        q2.description = "The lab is clear. Get to the elevator and make it to floor 2.";
        q2.isMainQuest = true;
        q2.objectives  = new SennaQuestObjective[]
        {
            new SennaQuestObjective { type = SennaObjectiveType.Interact, interactKey = "use_elevator", requiredCount = 1, shortLabel = "Take the elevator to Floor 2" }
        };
        SaveIfNew(q2, "Q2_FindElevator");

        // Side Mission — Resource Run
        var side = ScriptableObject.CreateInstance<SennaQuestData>();
        side.questName   = "Resource Run";
        side.description = "Scavenge supplies from the area. Eliminate any hostiles in your way.";
        side.isMainQuest = false;
        side.objectives  = new SennaQuestObjective[]
        {
            new SennaQuestObjective { type = SennaObjectiveType.KillEnemy, requiredCount = 3, shortLabel = "Eliminate targets" }
        };
        side.rewardPool = new SennaRewardEntry[]
        {
            new SennaRewardEntry { displayLabel = "Mutant Meat x2",  rewardItem = meat,  quantity = 2, weight = 40 },
            new SennaRewardEntry { displayLabel = "Scraps x3",       rewardItem = scrap, quantity = 3, weight = 35 },
            new SennaRewardEntry { displayLabel = "Medicine x1",     rewardItem = med,   quantity = 1, weight = 20 },
            new SennaRewardEntry { displayLabel = "Ammo x15",        ammoAmount = 15,                 weight = 5  }
        };
        SaveIfNew(side, "SideMission_ResourceRun");

        PatchLauranPrefab("Meat",  meat);
        PatchLauranPrefab("Med",   med);
        PatchLauranPrefab("Scrap", scrap);
        PatchLauranPrefab("Serum", note);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[QuestAssetCreator] Assets created. Now run Tools > Senna > Setup Quest Manager In Scene.");
    }

    // ── Fix Q1 kill count to 2 on an already-created asset ──────────────────

    [MenuItem("Tools/Senna/Fix Q1 Kill Count (set to 2)")]
    public static void FixQ1KillCount()
    {
        var q1 = AssetDatabase.LoadAssetAtPath<SennaQuestData>($"{QuestFolder}/Q1_InvestigateLab.asset");
        if (q1 == null) { Debug.LogError("[QuestAssetCreator] Q1_InvestigateLab.asset not found. Run Create Quest Assets first."); return; }

        var so = new SerializedObject(q1);
        var objectives = so.FindProperty("objectives");
        for (int i = 0; i < objectives.arraySize; i++)
        {
            var obj = objectives.GetArrayElementAtIndex(i);
            if ((SennaObjectiveType)obj.FindPropertyRelative("type").enumValueIndex == SennaObjectiveType.KillEnemy)
            {
                obj.FindPropertyRelative("requiredCount").intValue = 2;
                Debug.Log("[QuestAssetCreator] Q1 kill count set to 2.");
            }
        }
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    // ── Wire QuestManager in the active scene ────────────────────────────────

    [MenuItem("Tools/Senna/Setup Quest Manager In Scene")]
    public static void SetupQuestManagerInScene()
    {
        var qm = Object.FindFirstObjectByType<SennaQuestManager>(FindObjectsInactive.Include);
        if (qm == null) { Debug.LogError("[QuestAssetCreator] No SennaQuestManager in scene."); return; }

        var gc  = Object.FindFirstObjectByType<GridController>(FindObjectsInactive.Include);
        var ig  = Object.FindFirstObjectByType<ItemGrid>(FindObjectsInactive.Include);
        var pi  = Object.FindFirstObjectByType<SennaPlayerInteractor>(FindObjectsInactive.Include);
        var ammo = Object.FindFirstObjectByType<SennaAmmoSystem>(FindObjectsInactive.Include);

        var q1 = AssetDatabase.LoadAssetAtPath<SennaQuestData>($"{QuestFolder}/Q1_InvestigateLab.asset");
        var q2 = AssetDatabase.LoadAssetAtPath<SennaQuestData>($"{QuestFolder}/Q2_FindElevator.asset");

        if (q1 == null || q2 == null) { Debug.LogError("[QuestAssetCreator] Quest assets missing. Run Create Quest Assets first."); return; }

        var so = new SerializedObject(qm);

        // Quests array: only main quests — side missions are triggered separately, not active from the start
        var questsProp = so.FindProperty("quests");
        questsProp.arraySize = 2;
        questsProp.GetArrayElementAtIndex(0).objectReferenceValue = q1;
        questsProp.GetArrayElementAtIndex(1).objectReferenceValue = q2;

        if (gc   != null) so.FindProperty("gridController").objectReferenceValue   = gc;
        if (ig   != null) so.FindProperty("inventoryGrid").objectReferenceValue    = ig;
        if (pi   != null) so.FindProperty("playerInteractor").objectReferenceValue = pi;
        if (ammo != null) so.FindProperty("ammoSystem").objectReferenceValue       = ammo;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(qm);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[QuestAssetCreator] QuestManager wired: quests={questsProp.arraySize}, GridController={(gc != null ? gc.name : "NOT FOUND")}, ItemGrid={(ig != null ? ig.name : "NOT FOUND")}. Save the scene (Ctrl+S).");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ItemData GetOrCreateItemData(string assetName, int w, int h, bool stackable, int maxStack, ItemData.ItemType type)
    {
        string path     = $"{QuestFolder}/{assetName}.asset";
        var    existing = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (existing != null) return existing;

        var data          = ScriptableObject.CreateInstance<ItemData>();
        data.width        = w;
        data.height       = h;
        data.stackable    = stackable;
        data.maxStackSize = maxStack;
        data.itemType     = type;
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static void SaveIfNew(ScriptableObject asset, string assetName)
    {
        string path = $"{QuestFolder}/{assetName}.asset";
        if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
        {
            Debug.LogWarning($"[QuestAssetCreator] {assetName}.asset already exists — skipped. Delete it first to regenerate.");
            Object.DestroyImmediate(asset);
            return;
        }
        AssetDatabase.CreateAsset(asset, path);
    }

    private static void PatchLauranPrefab(string prefabName, ItemData itemData)
    {
        string path = $"{LauranPrefabs}/{prefabName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            Debug.LogWarning($"[QuestAssetCreator] Prefab not found at {path} — skipped.");
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            var root = scope.prefabContentsRoot;
            if (root.GetComponent<SennaQuestItem>() != null) { Debug.Log($"[QuestAssetCreator] {prefabName}.prefab already patched."); return; }

            var col = root.GetComponent<Collider>();
            if (col == null) { var s = root.AddComponent<SphereCollider>(); s.isTrigger = true; s.radius = 0.5f; }
            else col.isTrigger = true;

            var qi = root.AddComponent<SennaQuestItem>();
            var so = new SerializedObject(qi);
            so.FindProperty("itemData").objectReferenceValue = itemData;
            so.FindProperty("displayName").stringValue       = prefabName;
            so.ApplyModifiedProperties();
        }
        Debug.Log($"[QuestAssetCreator] Patched {prefabName}.prefab.");
    }
}
#endif
