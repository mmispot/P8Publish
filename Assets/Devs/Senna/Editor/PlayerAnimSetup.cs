using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// One-click rebuild of the player's fire/reload animation after the rig <-> animation split.
//
// The artist ships rigs (Models/) and animations (Animations/) as SEPARATE FBXs so re-exports
// never overwrite clips. This is a plain STATE-BASED setup, no avatar/retargeting: the rig and
// the animation are both Generic, so the separate-file clips bind to the rig by matching bone
// PATH (same source rig), and the scripts drive them purely by STATE NAME ("Fire"/"Reload"):
//
//   1. Import: force both rig and animation FBXs to Generic + No Avatar (raw bone paths).
//   2. Materialize the takes in each animation FBX into named, referenceable clips.
//   3. Build each controller: Idle (default) / Fire / Reload, each action -> Idle on exit time,
//      Write Defaults OFF (so an un-animated transform keeps its prefab value).
//   4. Point the arms Animator (on the rig) at its controller; avatar = None.
//   5. Wire the open scene: SennaGunFeel.fireAnimators + SennaGunReload.reloadAnimators.
//
// The clips' ROOT placement curves (position + scale) are stripped at import by PlayerAnimClipFix
// so the clips animate only the POSE — placement/size stays the prefab's job (the arms root is
// scaled 10x in front of the camera). Without that strip the clips would shrink the arms ~10x and
// snap them to the world origin.
//
// The gun mesh is parented into the arms skeleton (Gun_Attach bone), so it rides the arms
// animation and needs no Animator of its own. The gun controller is rebuilt anyway, ready if the
// gun ever needs INDEPENDENT animation (slide/hammer): add an Animator on the GunAlphaV1 node.
//
// Supersedes the older FireAnimationSetup / ReloadSetup tools (their asset paths are now stale).
public static class PlayerAnimSetup
{
    // --- Animation FBXs (the takes live here now) ---
    private const string ArmsAnimFbx = "Assets/Artists/Timo/Animations/Arms/Arms1.fbx";
    private const string GunAnimFbx  = "Assets/Artists/Timo/Animations/Guns/TT33/Gun1.fbx";

    // --- Rig FBXs (mesh + skeleton; the avatar source) ---
    private const string ArmsRigFbx = "Assets/Artists/Timo/Models/Arms/ArmAlphaV1.0.fbx";
    private const string GunRigFbx  = "Assets/Artists/Timo/Models/Guns/GunAlphaV1.fbx";

    // --- Controllers (arms recreated; gun rebuilt in place to keep its guid) ---
    private const string ArmsControllerPath = "Assets/Artists/Timo/Models/Arms/ArmAlphaV1_0.controller";
    private const string GunControllerPath  = "Assets/Artists/Timo/Models/Guns/GunAlphaV1.controller";

    // --- Prefab holding the arms Animator ---
    private const string ArmsPrefab = "Assets/Artists/Timo/Prefabs/Arms.prefab";

    // The armature node the clips are authored against. The separate-file clips bind by bone PATH
    // relative to the Animator and start at "Root" (the armature's child), so they only resolve when
    // the Animator sits on this node — NOT the model root one level up. (Diagnose Player Anim showed
    // 46/47 paths MISSING with the Animator on the model root.)
    private const string ArmRigNodeName = "ArmRig";

    // Exact take names per rig. Exact (suffix) match avoids the decoy takes in the FBX
    // (ArmsTT33_ReloadwD, GunTT33_Fire5, Arms_Idle, Arms_Run, the CameraAction tracks).
    private const string ArmsIdle = "ArmsTT33_Idle";
    private const string ArmsFire = "ArmsTT33_Fire";
    private const string ArmsReload = "ArmsTT33_Reload";
    private const string GunIdle = "GunTT33_Idle";
    private const string GunFire = "GunTT33_Fire";
    private const string GunReload = "GunTT33_Reload";

    // Trigger parameter names — must match the strings the scripts SetTrigger on.
    private const string FireTrigger = "Fire";
    private const string ReloadTrigger = "Reload";

    [MenuItem("Tools/Senna/Rebuild Player Anim")]
    public static void Run()
    {
        // 1. Import: both rig and animation are Generic with NO avatar (raw bone paths, path binding).
        EnsureGenericNoAvatar(ArmsRigFbx);
        EnsureGenericNoAvatar(GunRigFbx);
        EnsureGenericNoAvatar(ArmsAnimFbx);
        EnsureGenericNoAvatar(GunAnimFbx);

        // 2. Materialize the takes into named, referenceable clips, and loop the idle takes so the
        //    Idle state plays continuously instead of freezing on its last frame.
        MaterializeClips(ArmsAnimFbx);
        MaterializeClips(GunAnimFbx);
        SetClipLooping(ArmsAnimFbx, ArmsIdle);
        SetClipLooping(GunAnimFbx, GunIdle);

        // 3. Controllers: Idle (default) / Fire / Reload, each returning to Idle on exit time.
        var gunCtrl  = BuildController(GunControllerPath,  GunAnimFbx,  GunIdle,  GunFire,  GunReload);
        var armsCtrl = BuildController(ArmsControllerPath, ArmsAnimFbx, ArmsIdle, ArmsFire, ArmsReload);
        AssetDatabase.SaveAssets();

        // 4. Arms Animator on the rig: controller only, avatar cleared to None.
        int armsConfigured = ConfigureArmsAnimator(ArmsPrefab, armsCtrl);

        // 5. Scene wiring.
        int wired = WireScene();

        Debug.Log($"[PlayerAnimSetup] Done (no-avatar state setup). " +
                  $"Controllers: arms={(armsCtrl ? "built" : "FAILED")}, gun={(gunCtrl ? "rebuilt" : "FAILED")}. " +
                  $"Arms Animator configured: {armsConfigured}. {wired} animator(s) wired into the scene. " +
                  "Set SennaGunReload.reloadDuration to the reload clip length, then play test.");
    }

    // Both rig and animation import as Generic with NO avatar. Generic clips bind to the rig by
    // bone PATH; since the animation was authored on this same rig, the paths match and no avatar
    // (and no retargeting) is needed. NoAvatar keeps the clips' raw bone paths intact.
    private static void EnsureGenericNoAvatar(string fbx)
    {
        var importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"[PlayerAnimSetup] No ModelImporter at {fbx}.");
            return;
        }

        if (importer.animationType != ModelImporterAnimationType.Generic ||
            importer.avatarSetup != ModelImporterAvatarSetup.NoAvatar)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.SaveAndReimport();
        }
    }

    // Splits the FBX takes into named clips. Idempotent: only runs when there are no explicit
    // splits yet, so a manual setup is never clobbered.
    private static void MaterializeClips(string fbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        if (importer.clipAnimations == null || importer.clipAnimations.Length == 0)
        {
            importer.clipAnimations = importer.defaultClipAnimations;
            importer.SaveAndReimport();
        }
    }

    // Builds (or rebuilds in place) a controller with Idle (default) / Fire / Reload, each action
    // returning to Idle on exit time. Existing states are cleared first so a rerun is deterministic.
    private static AnimatorController BuildController(
        string controllerPath, string animFbx, string idleTake, string fireTake, string reloadTake)
    {
        var idle   = FindClip(animFbx, idleTake);
        var fire   = FindClip(animFbx, fireTake);
        var reload = FindClip(animFbx, reloadTake);
        if (idle == null || fire == null || reload == null)
        {
            Debug.LogError($"[PlayerAnimSetup] Missing clip(s) in {animFbx}: " +
                           $"idle={(idle ? "ok" : idleTake)}, fire={(fire ? "ok" : fireTake)}, reload={(reload ? "ok" : reloadTake)}.");
            return null;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                         ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        var sm = controller.layers[0].stateMachine;
        foreach (var cs in sm.states.ToArray())
            sm.RemoveState(cs.state);

        var idleState   = sm.AddState("Idle");   idleState.motion = idle;
        var fireState   = sm.AddState("Fire");   fireState.motion = fire;
        var reloadState = sm.AddState("Reload"); reloadState.motion = reload;

        // Write Defaults ON (Unity default), matching the old af2dd9b controllers: any property a
        // state doesn't animate is reset to its prefab value each frame. This is safe ONLY because
        // PlayerAnimClipFix strips the clips' root position+scale curves on import — so the arms keep
        // their prefab placement (10x scale, parented under the camera) and the clips drive only the
        // pose. WeaponSway owns the arms root; the Animator only poses the child bones.

        sm.defaultState = idleState;

        // Parameter-driven: SennaGunFeel/SennaGunReload SetTrigger("Fire"/"Reload"). AnyState picks
        // the trigger up from whatever is currently playing (Idle, or mid-Fire for a rapid re-kick),
        // then the state runs to its end and exits back to Idle. Rebuilt fresh each run.
        foreach (var p in controller.parameters.ToArray())
            controller.RemoveParameter(p);
        controller.AddParameter(FireTrigger,   AnimatorControllerParameterType.Trigger);
        controller.AddParameter(ReloadTrigger, AnimatorControllerParameterType.Trigger);

        AddAnyStateTrigger(sm, fireState,   FireTrigger);
        AddAnyStateTrigger(sm, reloadState, ReloadTrigger);
        AddExitTransition(fireState, idleState);
        AddExitTransition(reloadState, idleState);

        // Verification: after the strip the pose clips should carry NO scale curves. Log per clip.
        LogScaleCurves(controllerPath, idleTake, idle);
        LogScaleCurves(controllerPath, fireTake, fire);
        LogScaleCurves(controllerPath, reloadTake, reload);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    // Marks a take as looping (loopTime = 1) on the importer so an idle state plays continuously
    // instead of holding its last frame. Idempotent: only reimports when something actually changes.
    private static void SetClipLooping(string fbxPath, string take)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0) return;

        bool changed = false;
        for (int i = 0; i < clips.Length; i++)
        {
            bool isTake = clips[i].name == take
                       || clips[i].name.EndsWith("|" + take)
                       || clips[i].name.EndsWith(take);
            if (isTake && !clips[i].loopTime)
            {
                clips[i].loopTime = true;
                changed = true;
            }
        }

        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    // Exact take match. EndsWith on the full take name rejects the decoys (…_ReloadwD, …_Fire5)
    // since those have trailing characters; the clip imports as "ArmRig|ArmsTT33_Fire".
    private static AnimationClip FindClip(string fbxPath, string take)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview"))
            .FirstOrDefault(c => c.name == take || c.name.EndsWith("|" + take) || c.name.EndsWith(take));
    }

    // Reports whether a clip animates m_LocalScale and on which object paths. Generic (non-Euler)
    // bindings cover scale; we scan both curve sets. Purely diagnostic — changes nothing.
    private static void LogScaleCurves(string controllerPath, string take, AnimationClip clip)
    {
        if (clip == null) return;
        var bindings = AnimationUtility.GetCurveBindings(clip)
            .Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip))
            .Where(b => b.propertyName.Contains("m_LocalScale"))
            .Select(b => string.IsNullOrEmpty(b.path) ? "<root>" : b.path)
            .Distinct()
            .ToArray();

        if (bindings.Length == 0)
            Debug.Log($"[PlayerAnimSetup] Clip '{take}' has NO scale curves → PlayerAnimClipFix strip is working; arms keep prefab scale.");
        else
            Debug.LogWarning($"[PlayerAnimSetup] Clip '{take}' STILL animates scale on: {string.Join(", ", bindings)} " +
                             "→ PlayerAnimClipFix didn't strip it (reimport the FBX); arms will shrink.");
    }

    // AnyState -> target the instant `trigger` fires. canTransitionToSelf so a rapid re-trigger
    // (spamming the mouse / R) re-kicks the action instead of waiting for it to finish.
    private static void AddAnyStateTrigger(AnimatorStateMachine sm, AnimatorState target, string trigger)
    {
        var t = sm.AddAnyStateTransition(target);
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = 0f;
        t.canTransitionToSelf = true;
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to)
    {
        if (from == null || to == null || from == to) return;
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.hasFixedDuration = true;
        t.duration = 0.05f;
        t.canTransitionToSelf = true;
    }

    // Sets the controller on the arms Animator (on the rig) and clears its avatar to None — the
    // Generic clips bind by bone path, no avatar needed. Targets the Animator already using the
    // arms controller, falling back to a null-controller Animator. Edits the prefab asset so every
    // instance updates. (Skips the nested gun mesh, which has no Animator.)
    private static int ConfigureArmsAnimator(string prefabPath, RuntimeAnimatorController ctrl)
    {
        if (ctrl == null) return 0;

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        int count = 0;
        try
        {
            var animators = root.GetComponentsInChildren<Animator>(true);
            var target = animators.FirstOrDefault(a => a.runtimeAnimatorController == ctrl)
                      ?? animators.FirstOrDefault(a => a.runtimeAnimatorController == null);

            if (target != null)
            {
                target.runtimeAnimatorController = ctrl;
                target.avatar = null;          // no-avatar setup: Generic path binding
                target.applyRootMotion = false;
                count++;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            else
            {
                Debug.LogWarning($"[PlayerAnimSetup] No arms Animator found in {prefabPath}.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return count;
    }

    // Scene wiring: feed the player Animator(s) into SennaGunFeel.fireAnimators and
    // SennaGunReload.reloadAnimators, and SchootingRaycast.reload.
    private static int WireScene()
    {
        var gunFeel = Object.FindFirstObjectByType<SennaGunFeel>(FindObjectsInactive.Include);
        if (gunFeel == null)
        {
            Debug.LogWarning("[PlayerAnimSetup] No SennaGunFeel in the open scene — open the player scene " +
                             "(Quests.unity) and rerun, or wire the animator arrays by hand.");
            return 0;
        }

        var playerRoot = gunFeel.transform.root.gameObject;
        var animators = playerRoot.GetComponentsInChildren<Animator>(true);
        if (animators.Length == 0)
        {
            Debug.LogWarning("[PlayerAnimSetup] No Animators under the player — controllers built, but nothing wired.");
            return 0;
        }

        WriteAnimatorArray(gunFeel, "fireAnimators", animators);

        var reload = gunFeel.GetComponent<SennaGunReload>() ?? Undo.AddComponent<SennaGunReload>(gunFeel.gameObject);
        WriteAnimatorArray(reload, "reloadAnimators", animators);

        var shooter = Object.FindFirstObjectByType<SchootingRaycast>(FindObjectsInactive.Include);
        if (shooter != null)
        {
            var so = new SerializedObject(shooter);
            var rp = so.FindProperty("reload");
            if (rp != null) { rp.objectReferenceValue = reload; so.ApplyModifiedProperties(); }
            EditorUtility.SetDirty(shooter);
        }

        EditorUtility.SetDirty(gunFeel);
        EditorUtility.SetDirty(reload);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return animators.Length;
    }

    private static void WriteAnimatorArray(Object target, string propName, Animator[] animators)
    {
        var so = new SerializedObject(target);
        var arr = so.FindProperty(propName);
        if (arr == null) return;
        arr.arraySize = animators.Length;
        for (int i = 0; i < animators.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = animators[i];
        so.ApplyModifiedProperties();
    }

    // ----- Root-cause fix for "the clip plays but nothing moves" -----
    // No-avatar Generic clips bind by bone PATH relative to the Animator. These clips' paths start at
    // "Root" (the armature's child), but the Animator was added one level up on the model root, so the
    // real paths are "ArmRig/Root/..." and every bone binding went MISSING. Moving the Animator onto
    // the ArmRig armature node makes "Root/..." resolve directly. Idempotent; confirm afterwards with
    // Tools > Senna > Diagnose Player Anim (Fire/Reload should show 0 MISSING).
    [MenuItem("Tools/Senna/Move Arms Animator To Rig")]
    public static void MoveArmsAnimatorToRig()
    {
        bool moved = RelocateAnimatorToNode(ArmsPrefab, ArmRigNodeName);

        // The prefab edit propagates to the open scene's instance on the next editor tick; defer the
        // re-wire so WireScene picks up the Animator at its NEW node, not the destroyed old one.
        EditorApplication.delayCall += () =>
        {
            int wired = WireScene();
            Debug.Log($"[PlayerAnimSetup] Move Arms Animator -> '{ArmRigNodeName}': " +
                      $"{(moved ? "moved" : "already there / nothing to move")}. Re-wired {wired} animator(s) " +
                      "into the scene. Re-run Tools > Senna > Diagnose Player Anim — Fire/Reload should now " +
                      "show 0 MISSING — then play test (fire = mouse, reload = R).");
        };
    }

    // Moves the arms Animator (an added component on the model root) onto `nodeName` — the armature
    // node the clips are authored against — copying its controller/avatar/settings. Edits the prefab
    // asset so every instance updates. No-op if an Animator already sits on the target node.
    private static bool RelocateAnimatorToNode(string prefabPath, string nodeName)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var target = FindDescendant(root.transform, nodeName);
            if (target == null)
            {
                Debug.LogError($"[PlayerAnimSetup] Node '{nodeName}' not found under {prefabPath} — nothing moved.");
                return false;
            }
            if (target.GetComponent<Animator>() != null)
                return false; // already on the rig node — idempotent rerun

            // The arms Animator is the first one ABOVE the target (the gun has its own, deeper one).
            Animator src = null;
            for (var t = target.parent; t != null && src == null; t = t.parent)
                src = t.GetComponent<Animator>();
            if (src == null)
            {
                Debug.LogError($"[PlayerAnimSetup] No Animator found above '{nodeName}' in {prefabPath}.");
                return false;
            }

            var dst = target.gameObject.AddComponent<Animator>();
            dst.runtimeAnimatorController = src.runtimeAnimatorController;
            dst.avatar          = src.avatar;
            dst.applyRootMotion = src.applyRootMotion;
            dst.updateMode      = src.updateMode;
            dst.cullingMode     = src.cullingMode;

            Object.DestroyImmediate(src);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            AssetDatabase.SaveAssets();
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform FindDescendant(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var found = FindDescendant(c, name);
            if (found != null) return found;
        }
        return null;
    }
}
