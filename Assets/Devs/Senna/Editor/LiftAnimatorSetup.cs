using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// Tools > Senna > Setup Lift Animator
// Select the lift root GameObject in the scene, then run this.
// Creates an AnimatorController with Open/Close states driven by the "IsOpen" bool,
// adds an Animator to the lift, and wires it into SennaLiftDoors automatically.
public static class LiftAnimatorSetup
{
    private const string ClipPath       = "Assets/Artists/yoeri/animations/Armature_elevator door open.anim";
    private const string ControllerPath = "Assets/Artists/yoeri/animations/LiftDoors.controller";

    [MenuItem("Tools/Senna/Setup Lift Animator")]
    public static void Run()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Setup Lift Animator", "Select the lift root GameObject first.", "OK");
            return;
        }

        var openClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (openClip == null)
        {
            EditorUtility.DisplayDialog("Setup Lift Animator", $"Could not find clip at:\n{ClipPath}", "OK");
            return;
        }

        // --- Build controller ---
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("IsOpen", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        // Closed state — no clip, rests at bind pose (doors shut)
        var closedState = sm.AddState("Closed");
        sm.defaultState = closedState;

        // Open state — plays the door-open clip and holds the last frame
        var openState = sm.AddState("Open");
        openState.motion = openClip;
        openState.speed  = 1f;

        // Close state — same clip played in reverse
        var closeState = sm.AddState("Closing");
        closeState.motion      = openClip;
        closeState.speed       = -1f;
        closeState.cycleOffset = 1f; // start from the end of the clip

        // Closed → Open  (IsOpen = true, instant)
        var toOpen = closedState.AddTransition(openState);
        toOpen.AddCondition(AnimatorConditionMode.If, 0, "IsOpen");
        toOpen.hasExitTime        = false;
        toOpen.duration           = 0f;
        toOpen.offset             = 0f;

        // Open → Closing  (IsOpen = false, instant)
        var toClose = openState.AddTransition(closeState);
        toClose.AddCondition(AnimatorConditionMode.IfNot, 0, "IsOpen");
        toClose.hasExitTime = false;
        toClose.duration    = 0f;

        // Closing → Closed  (after clip finishes playing backwards)
        var toClosed = closeState.AddTransition(closedState);
        toClosed.hasExitTime = true;
        toClosed.exitTime    = 1f; // normalized; with speed=-1 this fires when time hits 0
        toClosed.duration    = 0f;

        AssetDatabase.SaveAssets();

        // --- Animator goes on the lift ROOT so transform paths in the clip match ---
        // Remove any Animator that ended up on a child (e.g. Armature) from a previous run.
        foreach (var childAnimator in selected.GetComponentsInChildren<Animator>())
        {
            if (childAnimator.gameObject != selected)
            {
                Undo.DestroyObjectImmediate(childAnimator);
                Debug.Log($"[LiftAnimatorSetup] Removed stale Animator from child '{childAnimator.gameObject.name}'.");
            }
        }

        var animator = selected.GetComponent<Animator>();
        if (animator == null)
            animator = Undo.AddComponent<Animator>(selected);

        Undo.RecordObject(animator, "Assign Lift Controller");
        animator.runtimeAnimatorController = controller;

        // --- Wire SennaLiftDoors — move it to the root if it's on a child ---
        var doors = selected.GetComponent<P8Publish.Quests.SennaLiftDoors>();
        if (doors == null)
            doors = selected.GetComponentInChildren<P8Publish.Quests.SennaLiftDoors>();

        if (doors != null)
        {
            var so = new SerializedObject(doors);
            so.FindProperty("doorAnimator").objectReferenceValue = animator;
            so.ApplyModifiedProperties();
            Debug.Log("[LiftAnimatorSetup] Wired Animator into SennaLiftDoors.", doors);
        }
        else
        {
            Debug.LogWarning("[LiftAnimatorSetup] SennaLiftDoors not found on selected object — assign Animator manually.", selected);
        }

        EditorUtility.DisplayDialog("Setup Lift Animator",
            "Done!\n\nAnimator Controller created at:\n" + ControllerPath +
            "\n\nNext: drag MainPlayer into the Player field on SennaLiftDoors.", "OK");
    }
}
