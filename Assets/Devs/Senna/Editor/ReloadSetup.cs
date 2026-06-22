using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// One-click setup for the gun/arms reload (and fire) in the OPEN scene.
//
// Touches the AnimatorControllers on purpose — but follows the ONE rule that the earlier
// broken tool violated: it NEVER nulls an existing Idle/default state's motion. The arms
// FBX has only the reload clip, so blanking Idle there snapped the arms to bind pose.
// Here the existing Idle is reused as the return target and its motion is left untouched;
// a new empty Idle is only ever created for the GUN (empty = bind pose, fine for a held gun).
//
// Per controller it:
//   - finds the reload state (by name, case-insensitive) and renames it to "Reload"
//   - finds the fire state and renames it to "Fire"
//   - picks the Idle/return state, sets it as default (so nothing auto-fires)
//   - adds exit-time Reload->Idle and Fire->Idle transitions (idempotent)
// Then it wires the scene: SennaGunReload (next to SennaGunFeel) -> ammo + the same
// animators SennaGunFeel fires, and SchootingRaycast.reload so firing is blocked mid-reload.
public static class ReloadSetup
{
    private const string ArmsControllerPath = "Assets/Artists/Timo/Models/Arms/ArmAlphaV1_0.controller";
    private const string ArmsFbxPath        = "Assets/Artists/Timo/Models/Arms/ArmAlphaV1.0.fbx";
    private const string GunControllerPath  = "Assets/Artists/Timo/Models/Guns/GunAlphaV1.controller";

    [MenuItem("Tools/Senna/Setup Gun Reload")]
    public static void Run()
    {
        // Arms: never allowed to fabricate an Idle (would blank the arms). Can pull a
        // reload clip from the arms FBX if the state isn't there yet.
        bool armsOk = SetupController(ArmsControllerPath, allowCreateIdle: false, reloadClipFbx: ArmsFbxPath);
        // Gun: an empty Idle is safe (bind pose on a held gun).
        bool gunOk  = SetupController(GunControllerPath, allowCreateIdle: true, reloadClipFbx: null);

        AssetDatabase.SaveAssets();

        int animators = WireScene();

        Debug.Log($"[ReloadSetup] Done. Arms reload: {(armsOk ? "ok" : "no reload state")}, " +
                  $"gun reload: {(gunOk ? "ok" : "no reload state")}, {animators} animator(s) wired. " +
                  "No Idle motion was cleared. Set SennaGunReload.reloadDuration to the clip length, then play test.");
    }

    // Returns true if the controller ended up with a "Reload" state.
    private static bool SetupController(string controllerPath, bool allowCreateIdle, string reloadClipFbx)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[ReloadSetup] Controller not found at {controllerPath}.");
            return false;
        }

        var sm = controller.layers[0].stateMachine;
        var states = sm.states.Select(s => s.state).ToList();

        // --- Reload state: exact, then contains "reload", else create from the FBX clip ---
        var reloadState = states.FirstOrDefault(s => s.name == "Reload")
                       ?? states.FirstOrDefault(s => s.name.ToLowerInvariant().Contains("reload"));
        if (reloadState == null && reloadClipFbx != null)
        {
            var clip = LoadClip(reloadClipFbx, "Reload");
            if (clip != null)
            {
                reloadState = sm.AddState("Reload");
                reloadState.motion = clip;
            }
        }
        if (reloadState != null && reloadState.name != "Reload")
            reloadState.name = "Reload";

        // --- Fire state: exact, then contains "fire". Never created here. ---
        var fireState = states.FirstOrDefault(s => s.name == "Fire")
                     ?? states.FirstOrDefault(s => s.name.ToLowerInvariant().Contains("fire"));
        if (fireState != null && fireState.name != "Fire")
            fireState.name = "Fire";

        // --- Idle/return state. NEVER touch its motion. ---
        var idleState = states.FirstOrDefault(s => s != reloadState && s != fireState &&
                                                   s.name.ToLowerInvariant().Contains("idle"));
        if (idleState == null && sm.defaultState != null &&
            sm.defaultState != reloadState && sm.defaultState != fireState)
            idleState = sm.defaultState;                 // an existing non-action default works as Idle
        if (idleState == null && allowCreateIdle)
            idleState = sm.AddState("Idle");             // gun only — empty bind pose is fine
        if (idleState == null)
            Debug.LogWarning($"[ReloadSetup] '{controller.name}': no Idle state found and not creating one " +
                             "(would blank this rig). Reload/Fire will have nothing to return to — add a real Idle.");

        if (idleState != null)
        {
            sm.defaultState = idleState;                 // stop the rig auto-playing an action on entry
            AddExitTransition(reloadState, idleState);
            AddExitTransition(fireState, idleState);
        }

        EditorUtility.SetDirty(controller);
        return reloadState != null;
    }

    // Snappy exit-time return, no conditions. Idempotent; skips self-targets.
    private static void AddExitTransition(AnimatorState from, AnimatorState to)
    {
        if (from == null || to == null || from == to) return;
        if (from.transitions.Any(t => t.destinationState == to)) return;

        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.hasFixedDuration = true;
        t.duration = 0.05f;
    }

    private static AnimationClip LoadClip(string fbxPath, string nameContains)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview") &&
                                 c.name.ToLowerInvariant().Contains(nameContains.ToLowerInvariant()));
    }

    // Scene wiring only — no controller edits here.
    private static int WireScene()
    {
        var gunFeel = Object.FindFirstObjectByType<SennaGunFeel>(FindObjectsInactive.Include);
        if (gunFeel == null)
        {
            Debug.LogWarning("[ReloadSetup] No SennaGunFeel in the open scene — controllers were set up, " +
                             "but add SennaGunReload and assign its refs by hand.");
            return 0;
        }

        var go = gunFeel.gameObject;
        var reload = go.GetComponent<SennaGunReload>() ?? Undo.AddComponent<SennaGunReload>(go);

        // Reuse exactly the animators SennaGunFeel fires; fall back to the rig's Animators.
        Animator[] animators = ReadAnimators(new SerializedObject(gunFeel), "fireAnimators");
        if (animators.Length == 0)
            animators = go.GetComponentsInChildren<Animator>(true);

        var ammo = Object.FindFirstObjectByType<SennaAmmoSystem>(FindObjectsInactive.Include);

        var soReload = new SerializedObject(reload);
        var animProp = soReload.FindProperty("reloadAnimators");
        animProp.arraySize = animators.Length;
        for (int i = 0; i < animators.Length; i++)
            animProp.GetArrayElementAtIndex(i).objectReferenceValue = animators[i];
        if (ammo != null)
            soReload.FindProperty("ammo").objectReferenceValue = ammo;
        soReload.ApplyModifiedProperties();

        var shooter = Object.FindFirstObjectByType<SchootingRaycast>(FindObjectsInactive.Include);
        if (shooter != null)
        {
            var soShoot = new SerializedObject(shooter);
            var rp = soShoot.FindProperty("reload");
            if (rp != null)
            {
                rp.objectReferenceValue = reload;
                soShoot.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(shooter);
        }

        EditorUtility.SetDirty(reload);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return animators.Length;
    }

    private static Animator[] ReadAnimators(SerializedObject so, string propName)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || !prop.isArray) return new Animator[0];
        return Enumerable.Range(0, prop.arraySize)
            .Select(i => prop.GetArrayElementAtIndex(i).objectReferenceValue as Animator)
            .Where(a => a != null)
            .ToArray();
    }
}
