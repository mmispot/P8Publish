using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;

// One-click migration from the old arms rig (Grabthisone) to the new animated Arms model.
// - parents the new Arms under the player camera at the old rig's local pose
// - copies WeaponSway (with all tuned values) onto the new Arms root
// - copies SchootingRaycast onto the new gun and gives it a fresh firepoint
//   placed at the old muzzle's world position
// - rewires every scene reference (SennaGunFeel, GameStateManager, SennaPlayerMovement, ...)
// - deletes the old Grabthisone rig
public static class ArmsMigration
{
    private const string NewArmsPrefabPath = "Assets/Artists/Timo/Prefabs/Arms.prefab";

    [MenuItem("Tools/Senna/Migrate Arms To New Model")]
    public static void Run()
    {
        // --- Old rig: the WeaponSway that is NOT on an Arms.prefab instance ---
        WeaponSway oldSway = null;
        foreach (var sway in Object.FindObjectsByType<WeaponSway>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sway.gameObject) != NewArmsPrefabPath)
            {
                oldSway = sway;
                break;
            }
        }
        if (oldSway == null)
        {
            Debug.LogError("[ArmsMigration] No old WeaponSway found — already migrated, or the scene has no player.");
            return;
        }

        Transform oldRig = oldSway.transform;
        Transform cam = oldRig.parent; // Grabthisone is a camera child since the arms-rig fix
        if (cam == null || cam.GetComponentInChildren<Camera>(true) == null && cam.GetComponent<Camera>() == null)
        {
            Debug.LogError("[ArmsMigration] Old rig's parent is not the camera — run Tools > Senna > Parent Arms To Camera first.");
            return;
        }

        var oldShoot = oldRig.GetComponentInChildren<SchootingRaycast>(true);

        // --- New Arms: the scene instance of Arms.prefab ---
        GameObject newArms = FindNewArmsInstance();
        if (newArms == null)
        {
            Debug.LogError($"[ArmsMigration] No instance of {NewArmsPrefabPath} found in the scene — drag the Arms prefab in first.");
            return;
        }

        Undo.SetCurrentGroupName("Migrate Arms To New Model");
        int undoGroup = Undo.GetCurrentGroup();

        // --- 1. Parent under the camera at the old rig's pose (keep the prefab's own scale) ---
        if (newArms.transform.parent != cam)
        {
            Undo.SetTransformParent(newArms.transform, cam, "Parent new arms to camera");
            Undo.RecordObject(newArms.transform, "Place new arms");
            newArms.transform.localPosition = oldRig.localPosition;
            newArms.transform.localRotation = oldRig.localRotation;
        }

        // --- 2. Weapon layer so the overlay WeaponCamera renders it (and the main camera doesn't) ---
        SetLayerRecursive(newArms, oldRig.gameObject.layer);

        // --- 3. WeaponSway with all current tuned values ---
        var newSway = newArms.GetComponent<WeaponSway>();
        if (newSway == null)
        {
            ComponentUtility.CopyComponent(oldSway);
            ComponentUtility.PasteComponentAsNew(newArms);
            newSway = newArms.GetComponent<WeaponSway>();
            Undo.RegisterCreatedObjectUndo(newSway, "Add WeaponSway");
        }

        // --- 4. SchootingRaycast on the new gun + a fresh firepoint at the old muzzle ---
        SchootingRaycast newShoot = null;
        if (oldShoot != null)
        {
            Transform gunHost = FindChildByName(newArms.transform, "Guns") ?? newArms.transform;

            newShoot = gunHost.GetComponentInChildren<SchootingRaycast>(true);
            if (newShoot == null)
            {
                ComponentUtility.CopyComponent(oldShoot);
                ComponentUtility.PasteComponentAsNew(gunHost.gameObject);
                newShoot = gunHost.GetComponent<SchootingRaycast>();
                Undo.RegisterCreatedObjectUndo(newShoot, "Add SchootingRaycast");
            }

            var oldShootSO = new SerializedObject(oldShoot);
            var oldFirePoint = oldShootSO.FindProperty("firePoint").objectReferenceValue as Transform;

            var firePointGO = new GameObject("firepoint");
            Undo.RegisterCreatedObjectUndo(firePointGO, "Create firepoint");
            firePointGO.layer = oldRig.gameObject.layer;
            firePointGO.transform.SetParent(gunHost, false);
            if (oldFirePoint != null)
            {
                // Both rigs hang under the same camera right now, so world-space copy lands
                // the new muzzle exactly where the old one was
                firePointGO.transform.position = oldFirePoint.position;
                firePointGO.transform.rotation = oldFirePoint.rotation;
            }

            var newShootSO = new SerializedObject(newShoot);
            newShootSO.FindProperty("firePoint").objectReferenceValue = firePointGO.transform;
            newShootSO.ApplyModifiedProperties();
        }

        // --- 5. Rewire every scene reference from the old components to the new ones ---
        int rewired = RewireSceneReferences(oldSway, newSway, oldShoot, newShoot);

        // --- 6. Delete the old rig (recorded as a removed-GameObject override on the prefab instance) ---
        string oldName = oldRig.gameObject.name;
        try
        {
            Undo.DestroyObjectImmediate(oldRig.gameObject);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ArmsMigration] Could not delete '{oldName}' ({e.Message}) — deactivated it instead, remove it manually.");
            oldRig.gameObject.SetActive(false);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[ArmsMigration] Done. '{newArms.name}' replaced '{oldName}': WeaponSway + SchootingRaycast copied, " +
                  $"{rewired} reference(s) rewired, firepoint recreated at the old muzzle. " +
                  "Check the arms pose in Play Mode and save the scene.");
    }

    private static GameObject FindNewArmsInstance()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var go = t.gameObject;
            if (PrefabUtility.GetNearestPrefabInstanceRoot(go) == go &&
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == NewArmsPrefabPath)
                return go;
        }
        return null;
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name)
                return t;
        return null;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        Undo.RecordObject(go, "Set weapon layer");
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    // Walks every serialized field of every MonoBehaviour in the scene and swaps
    // references to the old components for the new ones. Catches SennaGunFeel,
    // GameStateManager, SennaPlayerMovement and anything added later.
    private static int RewireSceneReferences(WeaponSway oldSway, WeaponSway newSway,
                                             SchootingRaycast oldShoot, SchootingRaycast newShoot)
    {
        int count = 0;
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null || mb == oldSway || mb == newSway ||
                (Object)mb == oldShoot || (Object)mb == newShoot) continue;

            var so = new SerializedObject(mb);
            var prop = so.GetIterator();
            bool changed = false;

            while (prop.Next(true))
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;

                if (prop.objectReferenceValue == oldSway)
                {
                    prop.objectReferenceValue = newSway;
                    changed = true;
                    count++;
                }
                else if (oldShoot != null && prop.objectReferenceValue == oldShoot)
                {
                    prop.objectReferenceValue = newShoot;
                    changed = true;
                    count++;
                }
            }

            if (changed)
                so.ApplyModifiedProperties();
        }
        return count;
    }
}
