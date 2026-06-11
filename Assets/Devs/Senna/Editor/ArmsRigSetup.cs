using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Reparents the arms rig (the GameObject holding WeaponSway) under the player camera,
// preserving its current pose. With the camera as parent the arms inherit 100% of the
// view rotation, so the player can never look inside the mesh — WeaponSway then only
// adds small offsets for feel.
public static class ArmsRigSetup
{
    [MenuItem("Tools/Senna/Parent Arms To Camera")]
    public static void Run()
    {
        var swayInScene = Object.FindFirstObjectByType<WeaponSway>(FindObjectsInactive.Include);
        if (swayInScene == null)
        {
            Debug.LogError("[ArmsRigSetup] No WeaponSway found in the open scene.");
            return;
        }

        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(swayInScene.gameObject);

        if (string.IsNullOrEmpty(prefabPath))
        {
            // Plain scene object — reparent directly
            if (Reparent(swayInScene))
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return;
        }

        // Prefab instance: Unity forbids restructuring instances, so edit the asset itself
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var sway = root.GetComponentInChildren<WeaponSway>(true);
            if (sway == null)
            {
                Debug.LogError($"[ArmsRigSetup] No WeaponSway inside prefab {prefabPath}.");
                return;
            }

            if (Reparent(sway))
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[ArmsRigSetup] Done — '{sway.name}' is now a child of the camera in {prefabPath}. " +
                          "If the arms look offset in the scene, revert any leftover Transform overrides on the instance.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool Reparent(WeaponSway sway)
    {
        Transform cam = ResolveCamera(sway);
        if (cam == null)
        {
            Debug.LogError("[ArmsRigSetup] Could not find the player camera (SennaPlayerMovement.cameraTransform or a Camera component).");
            return false;
        }

        if (sway.transform.parent == cam)
        {
            Debug.Log("[ArmsRigSetup] Arms are already a child of the camera — nothing to do.");
            return false;
        }

        if (cam.IsChildOf(sway.transform))
        {
            Debug.LogError("[ArmsRigSetup] The camera is a child of the arms rig — fix the hierarchy manually first.");
            return false;
        }

        sway.transform.SetParent(cam, true); // worldPositionStays keeps the current pose
        return true;
    }

    private static Transform ResolveCamera(WeaponSway sway)
    {
        var movement = sway.GetComponentInParent<SennaPlayerMovement>(true);
        if (movement != null)
        {
            var so = new SerializedObject(movement);
            var cam = so.FindProperty("cameraTransform").objectReferenceValue as Transform;
            if (cam != null) return cam;

            var camComponent = movement.GetComponentInChildren<Camera>(true);
            if (camComponent != null) return camComponent.transform;
        }
        return null;
    }
}
