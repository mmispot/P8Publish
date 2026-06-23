using UnityEngine;
using UnityEditor;

// Adds SennaAimSystem to the player root and wires Camera, WeaponSway, and SennaPlayerMovement.
// Idempotent: safe to run more than once (skips components that are already wired).
public static class AimSystemSetup
{
    [MenuItem("Tools/Senna/Setup Aim System")]
    public static void SetupAimSystem()
    {
        var movement = Object.FindFirstObjectByType<SennaPlayerMovement>(FindObjectsInactive.Include);
        if (movement == null)
        {
            Debug.LogError("[AimSystemSetup] No SennaPlayerMovement found in the scene. Open the quest scene and try again.");
            return;
        }

        var playerRoot = movement.gameObject;

        // Add SennaAimSystem to player root if not already present
        var aim = playerRoot.GetComponent<SennaAimSystem>();
        if (aim == null)
        {
            aim = Undo.AddComponent<SennaAimSystem>(playerRoot);
            Debug.Log($"[AimSystemSetup] Added SennaAimSystem to '{playerRoot.name}'.");
        }
        else
        {
            Debug.Log($"[AimSystemSetup] SennaAimSystem already exists on '{playerRoot.name}' — updating references.");
        }

        // Find main camera (tagged MainCamera, not the weapon overlay camera)
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            // Fallback: find any Camera that isn't a child of itself
            var allCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCams)
            {
                if (c.CompareTag("MainCamera")) { mainCam = c; break; }
            }
        }

        // Find WeaponSway (on the arms rig, child of the camera)
        var sway = Object.FindFirstObjectByType<WeaponSway>(FindObjectsInactive.Include);

        var so = new SerializedObject(aim);

        if (mainCam != null)
        {
            so.FindProperty("playerCamera").objectReferenceValue = mainCam;
            Debug.Log($"[AimSystemSetup] Wired playerCamera → '{mainCam.gameObject.name}'.");
        }
        else
        {
            Debug.LogWarning("[AimSystemSetup] Could not find a Camera tagged 'MainCamera'. Assign playerCamera manually.");
        }

        if (sway != null)
        {
            so.FindProperty("weaponSway").objectReferenceValue = sway;
            Debug.Log($"[AimSystemSetup] Wired weaponSway → '{sway.gameObject.name}'.");
        }
        else
        {
            Debug.LogWarning("[AimSystemSetup] No WeaponSway found in scene. Assign weaponSway manually.");
        }

        so.FindProperty("playerMovement").objectReferenceValue = movement;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(aim);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[AimSystemSetup] Done. Hold RMB to aim. Tune 'Aim Position Offset' on WeaponSway and FOV values on SennaAimSystem to taste.");
    }
}
