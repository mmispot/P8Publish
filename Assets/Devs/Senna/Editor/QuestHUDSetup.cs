using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
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
            Debug.LogError("[QuestHUDSetup] No UICanvas found in the open scene.");
            return;
        }

        // This UI is fully generated — delete and rebuild so style changes land
        // on re-run. (Unity 6 records deleting prefab-instance children as a
        // removed-object override, so this is safe after applying to the prefab.)
        DestroyIfExists(canvasGO.transform, "QuestPanel");
        DestroyIfExists(canvasGO.transform, "InteractPrompt");

        // Built-in placeholder sprite; real art can be swapped in on the Images later.
        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // --- Quest panel (top-left): dark plate, title + main objectives + side quests ---
        var panelGO = new GameObject("QuestPanel");
        Undo.RegisterCreatedObjectUndo(panelGO, "Setup Quest HUD");
        panelGO.transform.SetParent(canvasGO.transform, false);
        panelGO.transform.SetSiblingIndex(0);

        var rt = panelGO.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(40, -40);
        rt.sizeDelta = new Vector2(380, 100);

        // Background on the panel itself: parents draw before children,
        // so the plate can never cover the text rows.
        var bg = panelGO.AddComponent<Image>();
        bg.sprite = uiSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.06f, 0.06f, 0.08f, 0.6f);
        bg.raycastTarget = false;

        var layout = panelGO.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 12);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Panel height hugs however many rows are active (SennaQuestHUD
        // deactivates empty rows, so the plate shrinks around them)
        var fitter = panelGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var title = CreateTextRow(panelGO.transform, "TitleText", 15, new Color(1f, 0.8f, 0.45f, 0.95f));
        title.fontStyle = FontStyles.Bold | FontStyles.UpperCase;

        var questText = CreateTextRow(panelGO.transform, "QuestText", 21, new Color(0.95f, 0.95f, 0.95f, 0.95f));

        var sideHeader = CreateTextRow(panelGO.transform, "SideHeader", 13, new Color(0.62f, 0.62f, 0.62f, 0.9f));
        sideHeader.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        sideHeader.text = "Side quests";

        var sideText = CreateTextRow(panelGO.transform, "SideQuestText", 16, new Color(0.72f, 0.72f, 0.72f, 0.85f));

        var hud = panelGO.AddComponent<SennaQuestHUD>();
        var so = new SerializedObject(hud);
        so.FindProperty("background").objectReferenceValue = bg;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("questText").objectReferenceValue = questText;
        so.FindProperty("sideHeader").objectReferenceValue = sideHeader.gameObject;
        so.FindProperty("sideQuestText").objectReferenceValue = sideText;
        so.ApplyModifiedProperties();

        // --- Interact prompt (center, lower third): text on an auto-sized dark pill ---
        var promptGO = new GameObject("InteractPrompt");
        Undo.RegisterCreatedObjectUndo(promptGO, "Setup Quest HUD");
        promptGO.transform.SetParent(canvasGO.transform, false);
        promptGO.transform.SetSiblingIndex(1);

        var promptRT = promptGO.AddComponent<RectTransform>();
        promptRT.anchorMin = promptRT.anchorMax = promptRT.pivot = new Vector2(0.5f, 0.5f);
        promptRT.anchoredPosition = new Vector2(0, -150);

        var promptBG = promptGO.AddComponent<Image>();
        promptBG.sprite = uiSprite;
        promptBG.type = Image.Type.Sliced;
        promptBG.color = new Color(0.05f, 0.05f, 0.07f, 0.7f);
        promptBG.raycastTarget = false;
        promptBG.enabled = false; // SennaPromptUI shows it together with the text

        var promptLayout = promptGO.AddComponent<HorizontalLayoutGroup>();
        promptLayout.padding = new RectOffset(18, 18, 8, 8);
        promptLayout.childControlWidth = true;
        promptLayout.childControlHeight = true;
        promptLayout.childForceExpandWidth = false;
        promptLayout.childForceExpandHeight = false;

        // Pill hugs the prompt text in both directions
        var promptFitter = promptGO.AddComponent<ContentSizeFitter>();
        promptFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        promptFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var promptText = CreateTextRow(promptGO.transform, "PromptText", 23, new Color(1f, 1f, 1f, 0.95f));
        promptText.alignment = TextAlignmentOptions.Center;

        var promptUI = promptGO.AddComponent<SennaPromptUI>();
        var pso = new SerializedObject(promptUI);
        pso.FindProperty("promptText").objectReferenceValue = promptText;
        pso.FindProperty("background").objectReferenceValue = promptBG;
        pso.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[QuestHUDSetup] Done. If UICanvas is a prefab instance, apply the overrides so QuestPanel and InteractPrompt end up in UICanvas.prefab.");
    }

    private static TextMeshProUGUI CreateTextRow(Transform parent, string rowName, float fontSize, Color color)
    {
        var go = new GameObject(rowName);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        text.text = "";
        return text;
    }

    private static void DestroyIfExists(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child == null) return;
        Undo.DestroyObjectImmediate(child.gameObject);
        Debug.Log($"[QuestHUDSetup] Rebuilt {childName} with the current style.");
    }
}
