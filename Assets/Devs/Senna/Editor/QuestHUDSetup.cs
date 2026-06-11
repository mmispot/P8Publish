using UnityEngine;
using UnityEditor;
using TMPro;

public static class QuestHUDSetup
{
    [MenuItem("Tools/Senna/Setup Quest HUD")]
    public static void SetupQuestHUD()
    {
        var canvasGO = GameObject.Find("UICanvas");
        if (canvasGO == null)
        {
            var anyCanvas = Object.FindFirstObjectByType<Canvas>();
            canvasGO = anyCanvas != null ? anyCanvas.gameObject : null;
        }
        if (canvasGO == null)
        {
            Debug.LogError("[QuestHUDSetup] No UICanvas found in the open scene. Run Tools > Senna > Setup Start Screen UI first.");
            return;
        }

        // --- Active quest text (top-left, small muted), behind all menu panels ---
        if (canvasGO.transform.Find("QuestPanel") != null)
        {
            Debug.Log("[QuestHUDSetup] QuestPanel already exists — leaving it as is.");
        }
        else
        {
            var panelGO = new GameObject("QuestPanel");
            Undo.RegisterCreatedObjectUndo(panelGO, "Setup Quest HUD");
            panelGO.transform.SetParent(canvasGO.transform, false);
            panelGO.transform.SetSiblingIndex(0);

            var rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40, -40);
            rt.sizeDelta = new Vector2(500, 200);

            var textGO = new GameObject("QuestText");
            textGO.transform.SetParent(panelGO.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = Vector2.zero;
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = new Color(0.78f, 0.78f, 0.78f, 0.75f);
            text.raycastTarget = false;
            text.text = "";

            var hud = panelGO.AddComponent<SennaQuestHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("questText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        // --- Pickup/interaction prompt (center, lower third — clear of the crosshair) ---
        if (canvasGO.transform.Find("InteractPrompt") != null)
        {
            Debug.Log("[QuestHUDSetup] InteractPrompt already exists — leaving it as is.");
        }
        else
        {
            var promptGO = new GameObject("InteractPrompt");
            Undo.RegisterCreatedObjectUndo(promptGO, "Setup Quest HUD");
            promptGO.transform.SetParent(canvasGO.transform, false);
            promptGO.transform.SetSiblingIndex(1);

            var rt = promptGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -150);
            rt.sizeDelta = new Vector2(600, 50);

            var textGO = new GameObject("PromptText");
            textGO.transform.SetParent(promptGO.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = Vector2.zero;
            text.fontSize = 26;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 1f, 1f, 0.9f);
            text.raycastTarget = false;
            text.text = "";

            var promptUI = promptGO.AddComponent<SennaPromptUI>();
            var so = new SerializedObject(promptUI);
            so.FindProperty("promptText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[QuestHUDSetup] Done. If UICanvas is a prefab instance, apply the overrides so QuestPanel and InteractPrompt end up in UICanvas.prefab.");
    }
}
