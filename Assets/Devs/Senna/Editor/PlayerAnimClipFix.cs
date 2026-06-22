using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Strips placement curves from the player's separate-file animation FBXs so each clip drives only
// the POSE (bone rotations), never the arms/gun root transform.
//
// Why this exists:
//   The rig and the animation now live in DIFFERENT FBXs (so artist re-exports never overwrite
//   clips). The arms prefab places the hands in front of the camera by scaling its root 10x. But
//   the animation FBX was authored at the rig's native (~1x) scale and origin, and Unity bakes the
//   root node's position + scale into every Generic clip. When the Animator plays a clip it
//   overwrites the prefab's 10x placement with those native values -> the arms shrink ~10x and snap
//   to the world origin (off-screen). That's the "tiny arms on the floor" bug.
//
//   Write Defaults = false does NOT fix it: these are explicit curves in the clip, not defaults.
//   The correct fix is to remove the placement curves at import time. Placement belongs to the
//   prefab/camera; the clip should only animate the pose. Runs on every import, so re-exports of
//   Arms1.fbx / Gun1.fbx stay safe automatically.
//
// Scope is deliberately narrow: only the two player animation FBXs, only scale curves (a hand/gun
// pose never needs scale) and only the ROOT node's position curve (finger/bone translations and ALL
// rotations are left untouched).
public class PlayerAnimClipFix : AssetPostprocessor
{
    private static readonly string[] Targets =
    {
        "Assets/Artists/Timo/Animations/Arms/Arms1.fbx",
        "Assets/Artists/Timo/Animations/Guns/TT33/Gun1.fbx",
    };

    private void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        if (!Targets.Contains(assetPath)) return;

        var bindings = AnimationUtility.GetCurveBindings(clip);
        if (bindings.Length == 0) return;

        // The placement (root) node = the shallowest animated transform path. Everything deeper is
        // an actual bone we must keep.
        string rootPath = bindings
            .Select(b => b.path)
            .OrderBy(p => p.Count(c => c == '/'))
            .ThenBy(p => p.Length)
            .First();

        var stripped = new List<string>();
        foreach (var b in bindings)
        {
            bool isScale   = b.propertyName.StartsWith("m_LocalScale");
            bool isRootPos = b.propertyName.StartsWith("m_LocalPosition") && b.path == rootPath;
            if (isScale || isRootPos)
            {
                AnimationUtility.SetEditorCurve(clip, b, null); // null curve removes the binding
                stripped.Add($"{(b.path == "" ? "<root>" : b.path)}.{b.propertyName}");
            }
        }

        if (stripped.Count > 0)
            Debug.Log($"[PlayerAnimClipFix] {System.IO.Path.GetFileName(assetPath)} / '{clip.name}': " +
                      $"stripped {stripped.Count} placement curve(s) " +
                      $"[{string.Join(", ", stripped.Distinct())}] " +
                      $"(root node = '{(rootPath == "" ? "<root>" : rootPath)}').");
    }

    // The postprocessor only runs on import, so force a reimport of both FBXs to apply it now (and
    // after any change to this script). Re-runnable.
    [MenuItem("Tools/Senna/Strip Anim Placement Curves")]
    public static void ForceReimport()
    {
        foreach (var path in Targets)
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        Debug.Log("[PlayerAnimClipFix] Reimported player animation FBXs — see the strip log above. " +
                  "Now play test: arms should keep their prefab size/position and only the pose animates.");
    }
}
