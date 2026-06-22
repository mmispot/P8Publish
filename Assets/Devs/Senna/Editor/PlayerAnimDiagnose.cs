using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// Read-only diagnosis of why the player's fire/reload animation may not be visibly playing.
// Run with the player scene open: Tools > Senna > Diagnose Player Anim.
//
// It answers the two questions that matter, so we stop guessing:
//   (A) Is the trigger/wiring set up?  -> are the animator arrays wired, do the controllers have
//       the Fire/Reload trigger params + states?
//   (B) Do the clips actually BIND to the bones?  -> for each state's clip, which curve binding
//       PATHS resolve to a real Transform under the Animator's GameObject vs go MISSING (a path
//       mismatch between the separate animation FBX and the rig hierarchy = the clip plays but
//       nothing moves). To make the mismatch obvious it also prints the rig's actual transform
//       hierarchy under the Animator and a sample of the clip's bind/missing paths side by side.
// It also reports whether each clip still carries ROOT position/rotation/scale curves (which would
// fight the prefab placement / explain a wrong FPS pose).
public static class PlayerAnimDiagnose
{
    [MenuItem("Tools/Senna/Diagnose Player Anim")]
    public static void Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== [PlayerAnimDiagnose] =====");

        var gunFeel = Object.FindFirstObjectByType<SennaGunFeel>(FindObjectsInactive.Include);
        var reload  = Object.FindFirstObjectByType<SennaGunReload>(FindObjectsInactive.Include);
        sb.AppendLine($"SennaGunFeel: {(gunFeel ? Path(gunFeel.transform) : "NOT FOUND")}");
        sb.AppendLine($"SennaGunReload: {(reload ? Path(reload.transform) : "NOT FOUND")}");

        ReportAnimatorArray(sb, gunFeel, "fireAnimators");
        ReportAnimatorArray(sb, reload, "reloadAnimators");

        var playerRoot = gunFeel ? gunFeel.transform.root.gameObject : null;
        if (playerRoot == null)
        {
            sb.AppendLine("No player root (SennaGunFeel missing) — open the player scene and rerun.");
            Debug.Log(sb.ToString());
            return;
        }

        foreach (var anim in playerRoot.GetComponentsInChildren<Animator>(true))
            ReportAnimator(sb, anim);

        sb.AppendLine("How to read this: a state whose clip shows MISSING paths is NOT binding to the");
        sb.AppendLine("rig (path mismatch) — that's why it plays but nothing moves. 0 MISSING = binds.");
        sb.AppendLine("Compare the MISSING paths against the 'rig hierarchy' list to see the difference.");
        Debug.Log(sb.ToString());
    }

    private static void ReportAnimatorArray(StringBuilder sb, Object owner, string prop)
    {
        if (owner == null) { sb.AppendLine($"  {prop}: (owner missing)"); return; }
        var so = new SerializedObject(owner);
        var arr = so.FindProperty(prop);
        if (arr == null) { sb.AppendLine($"  {prop}: (no such field)"); return; }
        sb.AppendLine($"  {prop}: {arr.arraySize} entr{(arr.arraySize == 1 ? "y" : "ies")}");
        for (int i = 0; i < arr.arraySize; i++)
        {
            var o = arr.GetArrayElementAtIndex(i).objectReferenceValue as Component;
            sb.AppendLine($"    [{i}] {(o ? Path(o.transform) : "NULL")}");
        }
    }

    private static void ReportAnimator(StringBuilder sb, Animator anim)
    {
        sb.AppendLine($"--- Animator @ '{Path(anim.transform)}' ---");
        sb.AppendLine($"    enabled={anim.isActiveAndEnabled}, avatar={(anim.avatar ? anim.avatar.name : "None")}, " +
                      $"controller={(anim.runtimeAnimatorController ? anim.runtimeAnimatorController.name : "NONE")}");

        if (!(anim.runtimeAnimatorController is AnimatorController ac))
        {
            sb.AppendLine("    (no AnimatorController asset to inspect)");
            return;
        }

        var paramNames = ac.parameters.Select(p => $"{p.name}:{p.type}").ToArray();
        sb.AppendLine($"    parameters: {(paramNames.Length == 0 ? "(none)" : string.Join(", ", paramNames))}");

        DumpHierarchy(sb, anim.transform);

        foreach (var layer in ac.layers)
            foreach (var cs in layer.stateMachine.states)
            {
                var clip = cs.state.motion as AnimationClip;
                sb.Append($"    state '{cs.state.name}': motion={(clip ? clip.name : (cs.state.motion ? cs.state.motion.name : "EMPTY"))}");
                if (clip == null) { sb.AppendLine(); continue; }
                AppendBindingReport(sb, anim.gameObject, clip);
            }
    }

    // Prints every transform PATH under the Animator (the real rig hierarchy, relative to the
    // Animator GameObject — the same space a clip's binding paths live in). Capped so a dense rig
    // can't flood the Console.
    private static void DumpHierarchy(StringBuilder sb, Transform animRoot)
    {
        var paths = new List<string>();
        CollectPaths(animRoot, animRoot, paths);
        sb.AppendLine($"    rig hierarchy under Animator ({paths.Count} transforms, relative paths):");
        const int cap = 80;
        for (int i = 0; i < paths.Count && i < cap; i++)
            sb.AppendLine($"        {paths[i]}");
        if (paths.Count > cap)
            sb.AppendLine($"        ... (+{paths.Count - cap} more)");
    }

    private static void CollectPaths(Transform root, Transform t, List<string> into)
    {
        foreach (Transform c in t)
        {
            into.Add(RelPath(root, c));
            CollectPaths(root, c, into);
        }
    }

    private static string RelPath(Transform root, Transform t)
    {
        var s = t.name;
        while (t.parent != null && t.parent != root) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    private static void AppendBindingReport(StringBuilder sb, GameObject animGo, AnimationClip clip)
    {
        var bindings = AnimationUtility.GetCurveBindings(clip);
        var resolvedPaths = new HashSet<string>();
        var missingPaths = new HashSet<string>();
        bool rootPos = false, rootRot = false, rootScale = false;

        foreach (var b in bindings)
        {
            var t = string.IsNullOrEmpty(b.path) ? animGo.transform : animGo.transform.Find(b.path);
            string shown = string.IsNullOrEmpty(b.path) ? "<root>" : b.path;
            if (t != null) resolvedPaths.Add(shown);
            else missingPaths.Add(shown);

            bool isRootLevel = string.IsNullOrEmpty(b.path) || !b.path.Contains("/");
            if (isRootLevel)
            {
                if (b.propertyName.StartsWith("m_LocalPosition")) rootPos = true;
                if (b.propertyName.StartsWith("m_LocalRotation") || b.propertyName.StartsWith("localEuler")) rootRot = true;
                if (b.propertyName.StartsWith("m_LocalScale")) rootScale = true;
            }
        }

        sb.AppendLine($"  ->  {resolvedPaths.Count} transform path(s) BIND, {missingPaths.Count} MISSING" +
                      $"  | root curves pos={rootPos} rot={rootRot} scale={rootScale}");
        AppendPathSample(sb, "MISSING (clip drives this path, rig has no such transform)", missingPaths);
        if (missingPaths.Count > 0)
            AppendPathSample(sb, "BIND (resolved OK)", resolvedPaths);
    }

    private static void AppendPathSample(StringBuilder sb, string label, HashSet<string> paths)
    {
        if (paths.Count == 0) return;
        const int cap = 12;
        var sample = paths.OrderBy(p => p).Take(cap).ToArray();
        sb.AppendLine($"        {label}: {string.Join("  |  ", sample)}" +
                      (paths.Count > cap ? $"  ... (+{paths.Count - cap} more)" : ""));
    }

    private static string Path(Transform t)
    {
        var s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
