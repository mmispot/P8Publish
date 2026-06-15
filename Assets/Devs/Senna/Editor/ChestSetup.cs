using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// One-click idempotent chest setup for the Main Scene: turns the selected
// object (or one named "Cube") into a SennaChest that opens the Inventory
// canvas' CHEST panel + the player's InventoryGrid on F, through the same
// SennaPlayerInteractor system the quest items use. Safe to run repeatedly.
public static class ChestSetup
{
    private const string ChestAssetFolder = "Assets/Devs/Senna/Chests";
    private const string ChestDataPath = ChestAssetFolder + "/Chest_Default.asset";

    [MenuItem("Tools/Senna/Setup Chest")]
    public static void SetupChest()
    {
        // --- Target: selection first, then a scene object named "Cube" ---
        GameObject cube = Selection.activeGameObject;
        if (cube == null) cube = GameObject.Find("Cube");
        if (cube == null)
        {
            Debug.LogError("[ChestSetup] Select the chest object in the Hierarchy (or name it 'Cube') and run again.");
            return;
        }

        // --- Scene pieces the chest depends on ---
        GameObject chestPanel = FindInSceneIncludingInactive("CHEST");
        if (chestPanel == null)
        {
            Debug.LogError("[ChestSetup] No 'CHEST' panel found — is the Inventory canvas (Emilia's Canvas prefab) in the scene?");
            return;
        }

        // The player's InventoryGrid is CHEST's sibling; CHEST contains its own
        // grid also named InventoryGrid, so search from the shared parent.
        Transform inventoryGrid = chestPanel.transform.parent != null
            ? chestPanel.transform.parent.Find("InventoryGrid")
            : null;
        if (inventoryGrid == null)
            Debug.LogWarning("[ChestSetup] No 'InventoryGrid' next to CHEST — the player inventory won't open with the chest.");

        var gridController = Object.FindFirstObjectByType<GridController>(FindObjectsInactive.Include);
        var playerMovement = Object.FindFirstObjectByType<SennaPlayerMovement>(FindObjectsInactive.Include);
        var shooting       = Object.FindFirstObjectByType<SchootingRaycast>(FindObjectsInactive.Include);
        if (gridController == null)
            Debug.LogWarning("[ChestSetup] No GridController in the scene — the grids will NRE when opened. Add Emilia's inventory setup first.");
        if (playerMovement == null)
            Debug.LogWarning("[ChestSetup] No SennaPlayerMovement found — is the player in the scene?");

        // --- Interactor (the quest F-key system) on the player ---
        if (playerMovement != null
            && Object.FindFirstObjectByType<SennaPlayerInteractor>(FindObjectsInactive.Include) == null)
        {
            var interactor = Undo.AddComponent<SennaPlayerInteractor>(playerMovement.gameObject);
            var so = new SerializedObject(interactor);
            so.FindProperty("playerCamera").objectReferenceValue =
                playerMovement.GetComponentInChildren<Camera>(true);
            so.ApplyModifiedProperties();
            Debug.Log("[ChestSetup] Added SennaPlayerInteractor to the player.");
        }

        // --- Quest manager (the prompt UI reads the prompt through it) ---
        var questManager = Object.FindFirstObjectByType<SennaQuestManager>(FindObjectsInactive.Include);
        if (questManager == null)
        {
            var managerGO = new GameObject("QuestManager");
            Undo.RegisterCreatedObjectUndo(managerGO, "Setup Chest");
            questManager = managerGO.AddComponent<SennaQuestManager>();
            Debug.Log("[ChestSetup] Created QuestManager (empty quest list is fine).");
        }
        var qmSO = new SerializedObject(questManager);
        if (qmSO.FindProperty("playerInteractor").objectReferenceValue == null)
        {
            qmSO.FindProperty("playerInteractor").objectReferenceValue =
                Object.FindFirstObjectByType<SennaPlayerInteractor>(FindObjectsInactive.Include);
            qmSO.ApplyModifiedProperties();
        }

        if (Object.FindFirstObjectByType<SennaPromptUI>(FindObjectsInactive.Include) == null)
            Debug.LogWarning("[ChestSetup] No prompt UI in the scene — run Tools > Senna > Setup Quest HUD to get the '[F] Open ...' text.");

        // --- Chest data asset ---
        if (!AssetDatabase.IsValidFolder(ChestAssetFolder))
            AssetDatabase.CreateFolder("Assets/Devs/Senna", "Chests");
        var data = AssetDatabase.LoadAssetAtPath<SennaChestData>(ChestDataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<SennaChestData>();
            data.chestName = "Chest";
            data.openOnce = false; // grid chests reopen
            AssetDatabase.CreateAsset(data, ChestDataPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[ChestSetup] Created " + ChestDataPath);
        }

        // --- Components on the chest object ---
        // A solid (non-trigger) collider is correct here: the interact ray hits
        // non-triggers too, and bullets should stop on a chest.
        if (cube.GetComponent<Collider>() == null)
            Undo.AddComponent<BoxCollider>(cube);

        var chest = cube.GetComponent<SennaChest>();
        if (chest == null) chest = Undo.AddComponent<SennaChest>(cube);
        var chestSO = new SerializedObject(chest);
        if (chestSO.FindProperty("chestData").objectReferenceValue == null)
        {
            chestSO.FindProperty("chestData").objectReferenceValue = data;
            chestSO.ApplyModifiedProperties();
        }

        var ui = cube.GetComponent<SennaChestGridUI>();
        if (ui == null) ui = Undo.AddComponent<SennaChestGridUI>(cube);
        var uiSO = new SerializedObject(ui);
        uiSO.FindProperty("chestGridPanel").objectReferenceValue = chestPanel;
        uiSO.FindProperty("inventoryPanel").objectReferenceValue =
            inventoryGrid != null ? inventoryGrid.gameObject : null;
        uiSO.FindProperty("playerMovement").objectReferenceValue = playerMovement;
        uiSO.FindProperty("shooting").objectReferenceValue = shooting;
        uiSO.ApplyModifiedProperties();

        // --- GridInteract.mainCamera is null in the prefab and its Awake NREs
        //     without it; wire every grid inside CHEST to the GridController ---
        if (gridController != null)
        {
            foreach (var gi in chestPanel.GetComponentsInChildren<GridInteract>(true))
            {
                if (gi.mainCamera != null) continue;
                Undo.RecordObject(gi, "Setup Chest");
                gi.mainCamera = gridController.gameObject;
                PrefabUtility.RecordPrefabInstancePropertyModifications(gi);
                Debug.Log("[ChestSetup] Wired GridInteract.mainCamera on " + gi.gameObject.name + ".");
            }
        }

        EditorSceneManager.MarkSceneDirty(cube.scene);
        Debug.Log("[ChestSetup] Done. '" + cube.name + "' opens the chest grid + inventory on F. Save the scene (Ctrl+S).");
    }

    // GameObject.Find skips inactive objects; the CHEST panel starts inactive.
    private static GameObject FindInSceneIncludingInactive(string name)
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindRecursive(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static Transform FindRecursive(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var found = FindRecursive(t.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
