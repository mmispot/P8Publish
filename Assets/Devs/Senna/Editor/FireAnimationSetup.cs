using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// Prepares the arms + gun animator controllers for code-driven fire animations:
// - each controller gets a state named exactly "Fire" holding its fire clip
// - an (empty if needed) "Idle" state is the default, so nothing fires on its own
//   (the gun controller shipped with the fire clip as its default state)
// - Fire transitions back to Idle on exit time, no parameters anywhere —
//   SennaGunFeel force-restarts the "Fire" state on both animators in the same frame
// Also wires the two scene Animators into SennaGunFeel.fireAnimators.
public static class FireAnimationSetup
{
    private const string ArmsControllerPath = "Assets/Artists/Timo/Models/Arms/ArmAlphaV1_0.controller";
    private const string ArmsFbxPath        = "Assets/Artists/Timo/Models/Arms/ArmAlphaV1.0.fbx";
    private const string GunControllerPath  = "Assets/Artists/Timo/Models/Guns/GunAlphaV1.controller";
    private const string NewArmsPrefabPath  = "Assets/Artists/Timo/Prefabs/Arms.prefab";

    [MenuItem("Tools/Senna/Setup Fire Animations")]
    public static void Run()
    {
        var armsFireClip = LoadClip(ArmsFbxPath, "Fire");
        if (armsFireClip == null)
        {
            Debug.LogError($"[FireAnimationSetup] No fire clip found in {ArmsFbxPath}.");
            return;
        }

        bool armsOk = EnsureFireSetup(ArmsControllerPath, armsFireClip);
        bool gunOk  = EnsureFireSetup(GunControllerPath, null); // gun's fire clip is already a state

        AssetDatabase.SaveAssets();

        int wired = WireSceneAnimators();

        Debug.Log($"[FireAnimationSetup] Done. Arms controller: {(armsOk ? "ok" : "FAILED")}, " +
                  $"gun controller: {(gunOk ? "ok" : "FAILED")}, {wired} animator(s) wired into SennaGunFeel. " +
                  "Both animators now have an Idle default and a 'Fire' state that code restarts per shot.");
    }

    private static AnimationClip LoadClip(string fbxPath, string nameContains)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview") && c.name.Contains(nameContains));
    }

    // Makes sure the controller has: a "Fire" state (created from fireClip when the
    // controller doesn't already contain one), an Idle default state, and a
    // Fire -> Idle exit-time transition. Safe to rerun.
    private static bool EnsureFireSetup(string controllerPath, AnimationClip fireClip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[FireAnimationSetup] Controller not found at {controllerPath}.");
            return false;
        }

        var sm = controller.layers[0].stateMachine;
        var states = sm.states.Select(s => s.state).ToList();

        // --- Fire state: prefer exact name, then anything containing "Fire", else create ---
        var fireState = states.FirstOrDefault(s => s.name == "Fire")
                     ?? states.FirstOrDefault(s => s.name.Contains("Fire"));
        if (fireState == null)
        {
            if (fireClip == null)
            {
                Debug.LogError($"[FireAnimationSetup] {controller.name} has no fire state and no clip was supplied.");
                return false;
            }
            fireState = sm.AddState("Fire");
            fireState.motion = fireClip;
        }
        else if (fireState.name != "Fire")
        {
            fireState.name = "Fire"; // one shared name = one shared hash in code
        }

        // --- Idle default state (empty state = bind pose, fine for the gun) ---
        var idleState = states.FirstOrDefault(s => s != fireState && s.name.Contains("Idle"));
        if (idleState == null)
            idleState = states.FirstOrDefault(s => s != fireState);
        if (idleState == null)
            idleState = sm.AddState("Idle");

        // The arms FBX ships with only a fire clip, and its "Idle" state pointed at
        // that same clip — so every shot played twice (Fire state, then "Idle").
        // An idle that shares the fire motion is never intentional: empty the state
        // so the arms hold their rest pose until a real idle clip exists.
        if (idleState.motion != null && idleState.motion == fireState.motion)
        {
            idleState.motion = null;
            Debug.LogWarning($"[FireAnimationSetup] '{controller.name}': Idle state was playing the fire clip — " +
                             "cleared it. Assign a real idle clip there when the artist makes one.");
        }

        sm.defaultState = idleState;

        // --- Fire -> Idle on exit time, snappy blend, no conditions ---
        if (!fireState.transitions.Any(t => t.destinationState == idleState))
        {
            var t = fireState.AddTransition(idleState);
            t.hasExitTime = true;
            t.exitTime = 1f;
            t.hasFixedDuration = true;
            t.duration = 0.05f;
        }

        EditorUtility.SetDirty(controller);
        return true;
    }

    private static int WireSceneAnimators()
    {
        GameObject arms = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject) == t.gameObject &&
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == NewArmsPrefabPath)
            {
                arms = t.gameObject;
                break;
            }
        }
        if (arms == null)
        {
            Debug.LogWarning("[FireAnimationSetup] No Arms instance in the scene — assign SennaGunFeel.fireAnimators manually.");
            return 0;
        }

        var animators = arms.GetComponentsInChildren<Animator>(true);
        var gunFeel = Object.FindFirstObjectByType<SennaGunFeel>(FindObjectsInactive.Include);
        if (gunFeel == null)
        {
            Debug.LogWarning("[FireAnimationSetup] No SennaGunFeel in the scene — assign fireAnimators manually.");
            return 0;
        }

        var so = new SerializedObject(gunFeel);
        var arr = so.FindProperty("fireAnimators");
        arr.arraySize = animators.Length;
        for (int i = 0; i < animators.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = animators[i];
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return animators.Length;
    }
}
