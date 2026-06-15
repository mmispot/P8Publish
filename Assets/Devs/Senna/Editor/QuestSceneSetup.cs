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

        // --- Test items + quest assets ---
        if (!AssetDatabase.IsValidFolder(QuestAssetFolder))
            AssetDatabase.CreateFolder("Assets/Devs/Senna", "Quests");

        var powerCell = LoadOrCreateItem("Item_PowerCell");
        var scrap     = LoadOrCreateItem("Item_ScrapMetal");

        var mainQuest = LoadOrCreateQuest("Quest_Main_PowerCells", q =>
        {
            q.questName = "Power Cells";
            q.description = "Find the power cells scattered around the area.";
            q.isMainQuest = true;
            q.objectives = new[]
            {
                new SennaQuestObjective { type = SennaObjectiveType.CollectItem, targetItem = powerCell, requiredCount = 3, shortLabel = "Find power cells" }
            };
        });

        var sideQuest = LoadOrCreateQuest("Quest_Side_Scrap", q =>
        {
            q.questName = "Scrap Run";
            q.description = "Grab spare scrap metal for crafting.";
            q.isMainQuest = false;
            q.objectives = new[]
            {
                new SennaQuestObjective { type = SennaObjectiveType.CollectItem, targetItem = scrap, requiredCount = 2, shortLabel = "Collect scrap" }
            };
            q.rewardItems = new[] { scrap };
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
        questsProp.arraySize = 2;
        questsProp.GetArrayElementAtIndex(0).objectReferenceValue = mainQuest;
        questsProp.GetArrayElementAtIndex(1).objectReferenceValue = sideQuest;
        managerSO.FindProperty("playerInteractor").objectReferenceValue = interactor;
        managerSO.ApplyModifiedProperties();

        // --- Test world props ---
        if (GameObject.Find("QuestProps") != null)
        {
            Debug.Log("[QuestSceneSetup] QuestProps already exists — leaving it as is.");
        }
        else
        {
            var props = new GameObject("QuestProps");
            Undo.RegisterCreatedObjectUndo(props, "Setup Quests Scene");

            CreatePickup(props.transform, PrimitiveType.Capsule, "PowerCell_1", ground + new Vector3( 5f, 0.5f,  5f), powerCell, "Power Cell");
            CreatePickup(props.transform, PrimitiveType.Capsule, "PowerCell_2", ground + new Vector3(-6f, 0.5f,  3f), powerCell, "Power Cell");
            CreatePickup(props.transform, PrimitiveType.Capsule, "PowerCell_3", ground + new Vector3( 2f, 0.5f, -7f), powerCell, "Power Cell");
            CreatePickup(props.transform, PrimitiveType.Cube,    "Scrap_1",     ground + new Vector3(-3f, 0.3f, -5f), scrap,     "Scrap Metal");
            CreatePickup(props.transform, PrimitiveType.Cube,    "Scrap_2",     ground + new Vector3( 7f, 0.3f, -2f), scrap,     "Scrap Metal");

            // Solid (non-trigger) so it behaves like a physical terminal; the
            // interactor ray hits non-triggers too.
            var terminal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            terminal.name = "Terminal";
            terminal.transform.SetParent(props.transform, false);
            terminal.transform.position = ground + new Vector3(0f, 0.75f, 8f);
            terminal.transform.localScale = new Vector3(1f, 1.5f, 0.5f);
            var interactable = terminal.AddComponent<SennaInteractable>();
            var iso = new SerializedObject(interactable);
            iso.FindProperty("promptText").stringValue = "[F] Use terminal";
            iso.ApplyModifiedProperties();

            Debug.Log("[QuestSceneSetup] Spawned test pickups and terminal under QuestProps.");
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
        if (quest != null) return quest;

        quest = ScriptableObject.CreateInstance<SennaQuestData>();
        init(quest);
        AssetDatabase.CreateAsset(quest, path);
        Debug.Log($"[QuestSceneSetup] Created {path}.");
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
}
