using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;

// One-click chest setup. Keeps the existing SennaInteractable on the chest
// and wires its OnInteracted event to SennaChestGridUI.Open(), which shows
// the chest's ChestGrid child + the player's inventory canvas, freezes the
// player, and handles F/E to close. Safe to run repeatedly.
public static class ChestSetup
{
    [MenuItem("Tools/Senna/Setup Chest")]
    public static void SetupChest()
    {
        // --- Target: selection first, then fallback to "Cube" ---
        GameObject cube = Selection.activeGameObject;
        if (cube == null) cube = GameObject.Find("Cube");
        if (cube == null)
        {
            Debug.LogError("[ChestSetup] Select the chest object in the Hierarchy and run again.");
            return;
        }

        // --- Find the ChestGrid as a direct child of the chest ---
        var itemGrid = cube.GetComponentInChildren<ItemGrid>(true);
        if (itemGrid == null)
        {
            Debug.LogError("[ChestSetup] No ItemGrid (ChestGrid prefab) found as a child of '" + cube.name +
                           "'. Drag the ChestGrid prefab onto the chest in the Hierarchy first, then run again.");
            return;
        }
        GameObject chestPanel = itemGrid.gameObject;

        // --- Make ChestGrid renderable as UI (it needs a Canvas to render) ---
        if (chestPanel.GetComponentInParent<Canvas>() == null)
        {
            var canvas = chestPanel.GetComponent<Canvas>();
            if (canvas == null)
                canvas = Undo.AddComponent<Canvas>(chestPanel);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            if (chestPanel.GetComponent<GraphicRaycaster>() == null)
                Undo.AddComponent<GraphicRaycaster>(chestPanel);

            Debug.Log("[ChestSetup] Added Canvas + GraphicRaycaster to " + chestPanel.name + " so it renders as UI.");
        }

        // --- ChestGrid must start inactive (SennaChestGridUI.Open activates it) ---
        if (chestPanel.activeSelf)
        {
            Undo.RecordObject(chestPanel, "Setup Chest");
            chestPanel.SetActive(false);
        }

        // --- Wire GridInteract.mainCamera → GridController ---
        var gridController = Object.FindFirstObjectByType<GridController>(FindObjectsInactive.Include);
        if (gridController == null)
            Debug.LogWarning("[ChestSetup] No GridController in the scene — item dragging will NRE when the chest opens.");

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

        // --- Find the player's inventory canvas ---
        // Check InventoryPlayerBridge first (Senna's system), then InventoryManager (Emilia's system).
        GameObject inventoryCanvas = null;
        var bridge = Object.FindFirstObjectByType<InventoryPlayerBridge>(FindObjectsInactive.Include);
        if (bridge != null)
        {
            var bridgeSO = new SerializedObject(bridge);
            inventoryCanvas = bridgeSO.FindProperty("inventoryCanvas").objectReferenceValue as GameObject;
        }
        if (inventoryCanvas == null)
        {
            var invMgr = Object.FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
            if (invMgr != null) inventoryCanvas = invMgr.inventoryCanvas;
        }
        if (inventoryCanvas == null)
            Debug.LogWarning("[ChestSetup] No inventory canvas found — the player inventory won't open with the chest. " +
                             "Add InventoryPlayerBridge to the player with inventoryCanvas wired.");

        // --- Player / shooting references ---
        var playerMovement = Object.FindFirstObjectByType<SennaPlayerMovement>(FindObjectsInactive.Include);
        var shooting       = Object.FindFirstObjectByType<SchootingRaycast>(FindObjectsInactive.Include);
        if (playerMovement == null)
            Debug.LogWarning("[ChestSetup] No SennaPlayerMovement found — player won't freeze when chest opens.");

        // --- SennaPlayerInteractor on the player (provides the F-key prompt/raycast) ---
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

        if (Object.FindFirstObjectByType<SennaPromptUI>(FindObjectsInactive.Include) == null)
            Debug.LogWarning("[ChestSetup] No SennaPromptUI — run Tools > Senna > Setup Quest HUD to get the '[F] Open ...' prompt.");

        // --- Collider on the chest (SennaPlayerInteractor ray must hit it) ---
        if (cube.GetComponent<Collider>() == null)
            Undo.AddComponent<BoxCollider>(cube);

        // --- Keep SennaInteractable (the F-key interaction the team uses) ---
        var interactable = cube.GetComponent<SennaInteractable>();
        if (interactable == null)
            interactable = Undo.AddComponent<SennaInteractable>(cube);

        // --- Add SennaChestGridUI (handles open/close, cursor, player freeze) ---
        var ui = cube.GetComponent<SennaChestGridUI>();
        if (ui == null) ui = Undo.AddComponent<SennaChestGridUI>(cube);
        var uiSO = new SerializedObject(ui);
        uiSO.FindProperty("chestGridPanel").objectReferenceValue  = chestPanel;
        uiSO.FindProperty("inventoryPanel").objectReferenceValue  = inventoryCanvas;
        uiSO.FindProperty("playerMovement").objectReferenceValue  = playerMovement;
        uiSO.FindProperty("shooting").objectReferenceValue        = shooting;
        uiSO.ApplyModifiedProperties();

        // --- Wire SennaInteractable.onInteracted → SennaChestGridUI.Open() ---
        // Clear any stale SetActive calls from before, then add the correct one.
        Undo.RecordObject(interactable, "Setup Chest");
        var interactableSO = new SerializedObject(interactable);
        interactableSO.FindProperty("onInteracted.m_PersistentCalls.m_Calls").ClearArray();
        interactableSO.ApplyModifiedProperties();
        UnityEventTools.AddVoidPersistentListener(interactable.onInteracted, ui.Open);

        EditorSceneManager.MarkSceneDirty(cube.scene);
        Debug.Log("[ChestSetup] Done. '" + cube.name + "': F opens ChestGrid + inventory, F/E closes. Save the scene (Ctrl+S).");
    }
}
