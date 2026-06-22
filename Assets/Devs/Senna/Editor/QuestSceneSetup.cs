using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// One-click idempotent setup for the Quests scene: core prefabs, GameStateManager
// wiring, player interactor, test quest assets + world pickups, and the quest HUD.
// Safe to run repeatedly — every step checks for existing objects/assets first.
public static class QuestSceneSetup
{
    private const string PlayerPrefabPath = "Assets/Devs/Senna/FINALPLAYER/MainPlayer 1 1.prefab";
    private const string CanvasPrefabPath = "Assets/Devs/Senna/pefab UI/UICanvas.prefab";
    private const string GsmPrefabPath    = "Assets/Devs/Senna/pefab UI/GameStateManager.prefab";
    private const string QuestAssetFolder = "Assets/Devs/Senna/Quests";

    [MenuItem("Tools/Senna/Setup Quests Scene")]
    public static void SetupQuestsScene()
    {
        // Ground reference for placing the player and test props
        var plane = GameObject.Find("Plane");
        Vector3 ground = plane != null ? plane.transform.position : Vector3.zero;

        // --- Core prefabs ---
        var player   = FindOrInstantiate("MainPlayer 1 1", PlayerPrefabPath, ground + new Vector3(0f, 0.1f, 0f));
        var canvasGO = FindOrInstantiate("UICanvas", CanvasPrefabPath, Vector3.zero);
        var gsmGO    = FindOrInstantiate("GameStateManager", GsmPrefabPath, Vector3.zero);
        if (player == null || canvasGO == null || gsmGO == null) return;

        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Setup Quests Scene");
            Debug.Log("[QuestSceneSetup] Created EventSystem.");
        }

        // --- GameStateManager wiring (its Start NREs without the panels) ---
        var gsm = gsmGO.GetComponent<GameStateManager>();
        if (gsm == null)
        {
            Debug.LogError("[QuestSceneSetup] GameStateManager prefab has no GameStateManager component — wire it manually.");
        }
        else
        {
            var so = new SerializedObject(gsm);
            so.FindProperty("startPanel").objectReferenceValue   = FindChild(canvasGO, "StartPanel");
            so.FindProperty("pausePanel").objectReferenceValue   = FindChild(canvasGO, "PausePanel");
            so.FindProperty("confirmPanel").objectReferenceValue = FindChild(canvasGO, "ConfirmPanel");
            so.FindProperty("deathPanel").objectReferenceValue   = FindChild(canvasGO, "DeathPanel");
            so.FindProperty("playerActive").objectReferenceValue = player;
            so.FindProperty("playerMovement").objectReferenceValue = player.GetComponentInChildren<SennaPlayerMovement>(true);
            so.FindProperty("shooting").objectReferenceValue       = player.GetComponentInChildren<SchootingRaycast>(true);
            so.ApplyModifiedProperties();
        }

        // --- Re-link UICanvas internals that were lost when it was prefabbed ---
        WireCanvasToScene(canvasGO, gsm, player);

        // --- Interactor on the player ---
        var interactor = player.GetComponentInChildren<SennaPlayerInteractor>(true);
        if (interactor == null)
        {
            interactor = Undo.AddComponent<SennaPlayerInteractor>(player);
            var so = new SerializedObject(interactor);
            so.FindProperty("playerCamera").objectReferenceValue = player.GetComponentInChildren<Camera>(true);
            so.ApplyModifiedProperties();
            Debug.Log("[QuestSceneSetup] Added SennaPlayerInteractor to the player.");
        }

        // --- Quest assets ---
        if (!AssetDatabase.IsValidFolder(QuestAssetFolder))
            AssetDatabase.CreateFolder("Assets/Devs/Senna", "Quests");

        var briefingItem  = LoadOrCreateItem("Item_MissionBriefing");
        var ammoCrateItem = LoadOrCreateItem("Item_AmmoCrate");
        var intelItem     = LoadOrCreateItem("Item_IntelDocument");

        var q1 = LoadOrCreateQuest("Q1_SituationReport", q =>
        {
            q.questName   = "Situation Report";
            q.description = "Find the mission briefing to understand your objectives.";
            q.isMainQuest = true;
            q.objectives  = new[] { new SennaQuestObjective { type = SennaObjectiveType.CollectItem, targetItem = briefingItem, requiredCount = 1, shortLabel = "Find the briefing" } };
            q.rewardPool  = new[]
            {
                new SennaRewardEntry { displayLabel = "Ammo x15",      ammoAmount = 15, weight = 10 },
                new SennaRewardEntry { displayLabel = "Scrap Metal x2", ammoAmount = 0,  weight = 6  },
                new SennaRewardEntry { displayLabel = "Ammo x10",      ammoAmount = 10, weight = 4  },
            };
        });

        var q2 = LoadOrCreateQuest("Q2_ArmUp", q =>
        {
            q.questName   = "Arm Up";
            q.description = "Locate the ammo cache to resupply before pushing forward.";
            q.isMainQuest = true;
            q.objectives  = new[] { new SennaQuestObjective { type = SennaObjectiveType.CollectItem, targetItem = ammoCrateItem, requiredCount = 1, shortLabel = "Find the ammo cache" } };
            q.rewardPool  = new[]
            {
                new SennaRewardEntry { displayLabel = "Ammo x30",      ammoAmount = 30, weight = 10 },
                new SennaRewardEntry { displayLabel = "Ammo x20",      ammoAmount = 20, weight = 6  },
                new SennaRewardEntry { displayLabel = "Scrap Metal x3", ammoAmount = 0,  weight = 4  },
            };
        });

        var q3 = LoadOrCreateQuest("Q3_GetAccess", q =>
        {
            q.questName   = "Get Access";
            q.description = "Use the security terminal to unlock the next area.";
            q.isMainQuest = true;
            q.objectives  = new[] { new SennaQuestObjective { type = SennaObjectiveType.Interact, interactKey = "SecurityTerminal", requiredCount = 1, shortLabel = "Use the terminal" } };
            q.rewardPool  = new[]
            {
                new SennaRewardEntry { displayLabel = "Ammo x10",      ammoAmount = 10, weight = 10 },
                new SennaRewardEntry { displayLabel = "Scrap Metal x1", ammoAmount = 0,  weight = 8  },
                new SennaRewardEntry { displayLabel = "Ammo x15",      ammoAmount = 15, weight = 4  },
            };
        });

        var q4 = LoadOrCreateQuest("Q4_EnemyIntel", q =>
        {
            q.questName   = "Enemy Intel";
            q.description = "Recover the enemy intelligence document.";
            q.isMainQuest = true;
            q.objectives  = new[] { new SennaQuestObjective { type = SennaObjectiveType.CollectItem, targetItem = intelItem, requiredCount = 1, shortLabel = "Recover the intel" } };
            q.rewardPool  = new[]
            {
                new SennaRewardEntry { displayLabel = "Ammo x20",      ammoAmount = 20, weight = 10 },
                new SennaRewardEntry { displayLabel = "Ammo x30",      ammoAmount = 30, weight = 5  },
                new SennaRewardEntry { displayLabel = "Scrap Metal x4", ammoAmount = 0,  weight = 5  },
            };
        });

        var q5 = LoadOrCreateQuest("Q5_GetOut", q =>
        {
            q.questName   = "Get Out";
            q.description = "Activate the extraction beacon and get out.";
            q.isMainQuest = true;
            q.objectives  = new[] { new SennaQuestObjective { type = SennaObjectiveType.Interact, interactKey = "ExtractionBeacon", requiredCount = 1, shortLabel = "Activate extraction" } };
            q.rewardPool  = new[]
            {
                new SennaRewardEntry { displayLabel = "Ammo x50",      ammoAmount = 50, weight = 10 },
                new SennaRewardEntry { displayLabel = "Ammo x40",      ammoAmount = 40, weight = 6  },
                new SennaRewardEntry { displayLabel = "Scrap Metal x5", ammoAmount = 0,  weight = 4  },
            };
        });

        // --- Quest manager (re-wired every run; same values, so it stays idempotent) ---
        var managerGO = GameObject.Find("QuestManager");
        if (managerGO == null)
        {
            managerGO = new GameObject("QuestManager");
            Undo.RegisterCreatedObjectUndo(managerGO, "Setup Quests Scene");
            managerGO.AddComponent<SennaQuestManager>();
            Debug.Log("[QuestSceneSetup] Created QuestManager.");
        }
        var manager = managerGO.GetComponent<SennaQuestManager>();
        var managerSO = new SerializedObject(manager);
        var questsProp = managerSO.FindProperty("quests");
        questsProp.arraySize = 5;
        questsProp.GetArrayElementAtIndex(0).objectReferenceValue = q1;
        questsProp.GetArrayElementAtIndex(1).objectReferenceValue = q2;
        questsProp.GetArrayElementAtIndex(2).objectReferenceValue = q3;
        questsProp.GetArrayElementAtIndex(3).objectReferenceValue = q4;
        questsProp.GetArrayElementAtIndex(4).objectReferenceValue = q5;
        managerSO.FindProperty("playerInteractor").objectReferenceValue = interactor;
        managerSO.FindProperty("ammoSystem").objectReferenceValue = player.GetComponentInChildren<SennaAmmoSystem>(true);
        managerSO.ApplyModifiedProperties();

        // --- Game quest world props (5 objects matching the quest chain) ---
        if (GameObject.Find("GameQuestProps") != null)
        {
            Debug.Log("[QuestSceneSetup] GameQuestProps already exists — leaving it as is.");
        }
        else
        {
            var props = new GameObject("GameQuestProps");
            Undo.RegisterCreatedObjectUndo(props, "Setup Quests Scene");

            CreatePickup(props.transform, PrimitiveType.Capsule, "MissionBriefing", ground + new Vector3( 3f, 0.5f,  3f), briefingItem,  "Mission Briefing");
            CreatePickup(props.transform, PrimitiveType.Cube,    "AmmoCrate",       ground + new Vector3(-5f, 0.5f,  5f), ammoCrateItem, "Ammo Crate");
            CreateQuestInteractable(props.transform, "SecurityTerminal",  ground + new Vector3( 8f, 0.75f, 0f),  "SecurityTerminal",  "[F] Use terminal");
            CreatePickup(props.transform, PrimitiveType.Capsule, "IntelDocument",   ground + new Vector3( 0f, 0.5f, -6f), intelItem,     "Enemy Intel");
            CreateQuestInteractable(props.transform, "ExtractionBeacon",  ground + new Vector3(-8f, 0.5f, -4f), "ExtractionBeacon",  "[F] Activate beacon");

            Debug.Log("[QuestSceneSetup] Spawned 5 game quest props under GameQuestProps.");
        }

        // --- HUD ---
        QuestHUDSetup.SetupQuestHUD();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[QuestSceneSetup] Done. Apply UICanvas overrides (QuestPanel, InteractPrompt) and the MainPlayer 1 override (SennaPlayerInteractor) if you want them in the prefabs.");
    }

    // The UICanvas prefab can't store references to the GameStateManager or player
    // (separate prefabs), so button OnClick targets and the health UI's player refs
    // serialize as null. Method names survive, so we can retarget them here.
    private static void WireCanvasToScene(GameObject canvasGO, GameStateManager gsm, GameObject player)
    {
        if (gsm != null)
        {
            int retargeted = 0;
            foreach (var button in canvasGO.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var so = new SerializedObject(button);
                var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                bool changed = false;
                for (int i = 0; i < calls.arraySize; i++)
                {
                    var call = calls.GetArrayElementAtIndex(i);
                    var target = call.FindPropertyRelative("m_Target");
                    string method = call.FindPropertyRelative("m_MethodName").stringValue;
                    if (target.objectReferenceValue != null || string.IsNullOrEmpty(method)) continue;
                    if (typeof(GameStateManager).GetMethod(method) == null) continue;

                    target.objectReferenceValue = gsm;
                    call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue =
                        typeof(GameStateManager).AssemblyQualifiedName;
                    changed = true;
                    retargeted++;
                }
                if (changed) so.ApplyModifiedProperties();
            }
            if (retargeted > 0)
                Debug.Log($"[QuestSceneSetup] Retargeted {retargeted} button OnClick(s) to the scene GameStateManager.");
        }

        var playerHealth = player.GetComponentInChildren<SennaPlayerHealth>(true);
        var cameraShake  = player.GetComponentInChildren<SennaCameraShake>(true);

        foreach (var barUI in canvasGO.GetComponentsInChildren<SennaHealthBarUI>(true))
        {
            var so = new SerializedObject(barUI);
            if (so.FindProperty("playerHealth").objectReferenceValue == null)
            {
                so.FindProperty("playerHealth").objectReferenceValue = playerHealth;
                so.ApplyModifiedProperties();
                Debug.Log("[QuestSceneSetup] Wired SennaHealthBarUI to the player's health.");
            }
        }

        foreach (var feedback in canvasGO.GetComponentsInChildren<SennaDamageFeedback>(true))
        {
            var so = new SerializedObject(feedback);
            bool changed = false;
            if (so.FindProperty("playerHealth").objectReferenceValue == null)
            {
                so.FindProperty("playerHealth").objectReferenceValue = playerHealth;
                changed = true;
            }
            if (so.FindProperty("cameraShake").objectReferenceValue == null)
            {
                so.FindProperty("cameraShake").objectReferenceValue = cameraShake;
                changed = true;
            }
            if (changed)
            {
                so.ApplyModifiedProperties();
                Debug.Log("[QuestSceneSetup] Wired SennaDamageFeedback to the player.");
            }
        }
    }

    private static GameObject FindOrInstantiate(string objectName, string prefabPath, Vector3 position)
    {
        var existing = GameObject.Find(objectName);
        if (existing != null) return existing;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[QuestSceneSetup] Prefab not found at {prefabPath}.");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        Undo.RegisterCreatedObjectUndo(instance, "Setup Quests Scene");
        Debug.Log($"[QuestSceneSetup] Instantiated {objectName}.");
        return instance;
    }

    private static GameObject FindChild(GameObject parent, string childName)
    {
        var child = parent.transform.Find(childName);
        if (child == null)
        {
            Debug.LogWarning($"[QuestSceneSetup] {parent.name} has no child '{childName}' — wire it manually on GameStateManager.");
            return null;
        }
        return child.gameObject;
    }

    private static ItemData LoadOrCreateItem(string assetName)
    {
        string path = $"{QuestAssetFolder}/{assetName}.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null) return item;

        item = ScriptableObject.CreateInstance<ItemData>();
        item.itemType = ItemData.ItemType.Other;
        AssetDatabase.CreateAsset(item, path);
        Debug.Log($"[QuestSceneSetup] Created {path}.");
        return item;
    }

    private static SennaQuestData LoadOrCreateQuest(string assetName, System.Action<SennaQuestData> init)
    {
        string path = $"{QuestAssetFolder}/{assetName}.asset";
        var quest = AssetDatabase.LoadAssetAtPath<SennaQuestData>(path);
        bool isNew = quest == null;
        if (isNew)
            quest = ScriptableObject.CreateInstance<SennaQuestData>();

        // Always apply init so reward pool and objective changes land on re-runs.
        init(quest);

        if (isNew)
        {
            AssetDatabase.CreateAsset(quest, path);
            Debug.Log($"[QuestSceneSetup] Created {path}.");
        }
        else
        {
            EditorUtility.SetDirty(quest);
            Debug.Log($"[QuestSceneSetup] Updated {path}.");
        }
        return quest;
    }

    private static void CreatePickup(Transform parent, PrimitiveType shape, string objectName,
        Vector3 position, ItemData itemData, string displayName)
    {
        var go = GameObject.CreatePrimitive(shape);
        go.name = objectName;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.5f;

        // Trigger collider: the interactor ray uses QueryTriggerInteraction.Collide,
        // bullets use Ignore — so pickups are interactable but bullet-transparent.
        go.GetComponent<Collider>().isTrigger = true;

        var questItem = go.AddComponent<SennaQuestItem>();
        var so = new SerializedObject(questItem);
        so.FindProperty("itemData").objectReferenceValue = itemData;
        so.FindProperty("displayName").stringValue = displayName;
        so.ApplyModifiedProperties();
    }

    private static void CreateQuestInteractable(Transform parent, string objectName,
        Vector3 position, string questKey, string prompt)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(1f, 1.5f, 0.5f);

        // Trigger so the interaction ray hits it; no bullet collision needed.
        go.GetComponent<Collider>().isTrigger = true;

        var qi = go.AddComponent<SennaQuestInteractable>();
        var so = new SerializedObject(qi);
        so.FindProperty("questKey").stringValue = questKey;
        so.FindProperty("promptText").stringValue = prompt;
        so.ApplyModifiedProperties();
    }
}
