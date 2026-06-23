using UnityEngine;
using UnityEditor;
using TMPro;

// Builds the ammo readout into UICanvas and makes sure the gun actually tracks ammo.
// Mirrors HealthHUDSetup: idempotent, leaves an existing AmmoHUD alone (delete + rerun to rebuild).
public static class AmmoHUDSetup
{
    [MenuItem("Tools/Senna/Setup Ammo HUD")]
    public static void SetupAmmoHUD()
    {
        var canvasGO = GameObject.Find("UICanvas");
        if (canvasGO == null)
        {
            var anyCanvas = Object.FindFirstObjectByType<Canvas>();
            canvasGO = anyCanvas != null ? anyCanvas.gameObject : null;
        }
        if (canvasGO == null)
        {
            Debug.LogError("[AmmoHUDSetup] No UICanvas found in the open scene. Run Tools > Senna > Setup Start Screen UI first.");
            return;
        }

        // Make sure the player has a SennaAmmoSystem and that the shooter + reload reference it,
        // otherwise firing never consumes rounds and the HUD has nothing to show.
        var ammo = EnsureAmmoSystem();

        var existing = canvasGO.transform.Find("AmmoHUD");
        if (existing != null)
        {
            Debug.Log("[AmmoHUDSetup] AmmoHUD already exists — leaving it as is. Delete it and rerun to rebuild.");
        }
        else
        {
            var ammoGO = new GameObject("AmmoHUD");
            Undo.RegisterCreatedObjectUndo(ammoGO, "Setup Ammo HUD");
            ammoGO.transform.SetParent(canvasGO.transform, false);
            ammoGO.transform.SetSiblingIndex(0); // behind the menu panels, like the health bar

            var rt = ammoGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f); // bottom-right
            rt.anchoredPosition = new Vector2(-40f, 40f);
            rt.sizeDelta = new Vector2(240f, 64f);

            var text = ammoGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 40f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.BottomRight;
            text.color = new Color(0.95f, 0.93f, 0.88f, 1f);
            text.raycastTarget = false;
            text.text = ammo != null ? $"{ammo.CurrentInMag} / {ammo.ReserveAmmo}" : "12 / 60";

            var hud = ammoGO.AddComponent<SennaAmmoHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("ammo").objectReferenceValue = ammo;
            so.FindProperty("ammoText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[AmmoHUDSetup] Done. If UICanvas is a prefab instance, apply the overrides so AmmoHUD lands in UICanvas.prefab.");
    }

    // Finds the player's SennaAmmoSystem (adds one to the player root if missing) and wires
    // SchootingRaycast.ammo + SennaGunReload.ammo when they aren't already set.
    private static SennaAmmoSystem EnsureAmmoSystem()
    {
        var shooter = Object.FindFirstObjectByType<SchootingRaycast>(FindObjectsInactive.Include);
        var ammo = Object.FindFirstObjectByType<SennaAmmoSystem>(FindObjectsInactive.Include);

        if (ammo == null)
        {
            var host = shooter != null ? shooter.transform.root.gameObject : null;
            if (host == null)
            {
                Debug.LogWarning("[AmmoHUDSetup] No SchootingRaycast in the scene — couldn't place a SennaAmmoSystem. " +
                                 "Add one to the player and rerun.");
                return null;
            }
            ammo = Undo.AddComponent<SennaAmmoSystem>(host);
            Debug.Log($"[AmmoHUDSetup] Added a SennaAmmoSystem to '{host.name}'.");
        }

        WireAmmoReference(shooter, ammo);
        WireAmmoReference(Object.FindFirstObjectByType<SennaGunReload>(FindObjectsInactive.Include), ammo);
        return ammo;
    }

    private static void WireAmmoReference(Object target, SennaAmmoSystem ammo)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var p = so.FindProperty("ammo");
        if (p != null && p.objectReferenceValue == null)
        {
            p.objectReferenceValue = ammo;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }

    // -------------------------------------------------------------------------
    // Wires SennaAmmoSystem.inventoryGrid + ammoItemData so reserve ammo comes
    // from whatever Ammo stacks are sitting in the inventory grid.
    // Run this after the inventory canvas is in the Quest scene.
    // -------------------------------------------------------------------------
    [MenuItem("Tools/Senna/Wire Ammo Inventory")]
    public static void WireAmmoInventory()
    {
        const string AmmoAssetPath = "Assets/Devs/Emilia/Scripts/Inventory System/Scriptable Objects/Ammo.asset";

        var ammo = Object.FindFirstObjectByType<SennaAmmoSystem>(FindObjectsInactive.Include);
        if (ammo == null)
        {
            Debug.LogError("[WireAmmoInventory] No SennaAmmoSystem found in the scene. Run Tools > Senna > Setup Ammo HUD first.");
            return;
        }

        // Ensure the inventory prefab is in the scene — instantiate it if InventoryGrid is absent.
        const string InventoryPrefabPath = "Assets/Devs/Emilia/Scripts/Inventory System/INV PREFAB/Inventory.prefab";
        var inventoryGridGO = GameObject.Find("InventoryGrid");
        if (inventoryGridGO == null)
        {
            var invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefabPath);
            if (invPrefab == null)
            {
                Debug.LogError($"[WireAmmoInventory] No InventoryGrid in scene and couldn't find prefab at {InventoryPrefabPath}. Add the inventory prefab manually first.");
                return;
            }
            var invInstance = (GameObject)PrefabUtility.InstantiatePrefab(invPrefab);
            Undo.RegisterCreatedObjectUndo(invInstance, "Wire Ammo Inventory");
            invInstance.SetActive(false); // closed by default
            inventoryGridGO = GameObject.Find("InventoryGrid");
            Debug.Log("[WireAmmoInventory] Instantiated Inventory prefab.");
        }

        var grid = inventoryGridGO != null ? inventoryGridGO.GetComponent<ItemGrid>() : null;
        if (grid == null)
        {
            Debug.LogError("[WireAmmoInventory] Found 'InventoryGrid' but it has no ItemGrid component.");
            return;
        }

        var ammoData = AssetDatabase.LoadAssetAtPath<ItemData>(AmmoAssetPath);
        if (ammoData == null)
        {
            Debug.LogError($"[WireAmmoInventory] Could not load Ammo.asset at {AmmoAssetPath}. Check the path.");
            return;
        }

        // Find the GridController — its GameObject is also the mainCamera ref for GridInteract/InventoryManager.
        var gridController = Object.FindFirstObjectByType<GridController>(FindObjectsInactive.Include);
        if (gridController == null)
        {
            Debug.LogWarning("[WireAmmoInventory] No GridController found — make sure the inventory canvas is in the scene.");
            return;
        }

        // Ensure GridController has its item prefab assigned — without it spawning inventory items crashes.
        const string ItemPrefabPath = "Assets/Devs/Emilia/Scripts/Inventory System/Tests/Item.prefab";
        var gcSO = new SerializedObject(gridController);
        if (gcSO.FindProperty("itemPrefab").objectReferenceValue == null)
        {
            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath);
            if (itemPrefab != null)
            {
                gcSO.FindProperty("itemPrefab").objectReferenceValue = itemPrefab;
                gcSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(gridController);
                Debug.Log("[WireAmmoInventory] Assigned item prefab to GridController.");
            }
            else
            {
                Debug.LogError($"[WireAmmoInventory] Could not find item prefab at {ItemPrefabPath}. Assign it manually to GridController.");
                return;
            }
        }

        var so = new SerializedObject(ammo);
        so.FindProperty("inventoryGrid").objectReferenceValue   = grid;
        so.FindProperty("ammoItemData").objectReferenceValue    = ammoData;
        so.FindProperty("gridController").objectReferenceValue  = gridController;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ammo);

        // GridInteract and InventoryManager need the camera GameObject that holds GridController.
        var cameraGO = gridController.gameObject;
        foreach (var gi in Object.FindObjectsByType<GridInteract>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var giSO = new SerializedObject(gi);
            if (giSO.FindProperty("mainCamera").objectReferenceValue == null)
            {
                giSO.FindProperty("mainCamera").objectReferenceValue = cameraGO;
                giSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(gi);
            }
        }
        foreach (var im in Object.FindObjectsByType<InventoryManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var imSO = new SerializedObject(im);
            if (imSO.FindProperty("mainCamera").objectReferenceValue == null)
            {
                imSO.FindProperty("mainCamera").objectReferenceValue = cameraGO;
                imSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(im);
            }
        }
        Debug.Log($"[WireAmmoInventory] Wired GridController + mainCamera ('{cameraGO.name}') on all inventory components.");

        // InventoryManager expects Dominik's old PlayerMovement — disable it, InventoryPlayerBridge owns the toggle.
        var invManager = Object.FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
        if (invManager != null && invManager.enabled)
        {
            invManager.enabled = false;
            EditorUtility.SetDirty(invManager);
            Debug.Log($"[WireAmmoInventory] Disabled InventoryManager on '{invManager.gameObject.name}'.");
        }

        // The inventory canvas to toggle is the parent of InventoryGrid (e.g. "Inventory" GameObject).
        var inventoryCanvas = inventoryGridGO.transform.parent != null
            ? inventoryGridGO.transform.parent.gameObject
            : inventoryGridGO;

        // Add InventoryPlayerBridge to the player if missing.
        var playerMovement = Object.FindFirstObjectByType<SennaPlayerMovement>(FindObjectsInactive.Include);
        var bridge = Object.FindFirstObjectByType<InventoryPlayerBridge>(FindObjectsInactive.Include);
        if (bridge == null && playerMovement != null)
        {
            bridge = Undo.AddComponent<InventoryPlayerBridge>(playerMovement.transform.root.gameObject);
            Debug.Log($"[WireAmmoInventory] Added InventoryPlayerBridge to '{playerMovement.transform.root.name}'.");
        }

        if (bridge != null)
        {
            var shooter   = Object.FindFirstObjectByType<SchootingRaycast>(FindObjectsInactive.Include);
            var bSO = new SerializedObject(bridge);
            if (bSO.FindProperty("playerMovement").objectReferenceValue == null)
                bSO.FindProperty("playerMovement").objectReferenceValue = playerMovement;
            if (bSO.FindProperty("shooting").objectReferenceValue == null && shooter != null)
                bSO.FindProperty("shooting").objectReferenceValue = shooter;
            if (bSO.FindProperty("inventoryCanvas").objectReferenceValue == null)
                bSO.FindProperty("inventoryCanvas").objectReferenceValue = inventoryCanvas;
            bSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(bridge);
            Debug.Log($"[WireAmmoInventory] Wired InventoryPlayerBridge (canvas: '{inventoryCanvas.name}').");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[WireAmmoInventory] Done. SennaAmmoSystem on '{ammo.gameObject.name}' now reads reserve ammo from '{grid.gameObject.name}' using '{ammoData.name}'.");
    }
}
