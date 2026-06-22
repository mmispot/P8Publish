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
        rt.anchoredPosition = new Vector2(30, -30);
        rt.sizeDelta = new Vector2(460, 120);

        // Near-black background — Arc Raiders dark panel feel
        var bg = panelGO.AddComponent<Image>();
        bg.sprite = uiSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.04f, 0.04f, 0.06f, 0.92f);
        bg.raycastTarget = false;

        // Gold left accent bar (4 px, ignores layout so it overlays the full height)
        var accentGO = new GameObject("AccentBar");
        accentGO.transform.SetParent(panelGO.transform, false);
        var accentRT = accentGO.AddComponent<RectTransform>();
        accentRT.anchorMin = new Vector2(0f, 0f);
        accentRT.anchorMax = new Vector2(0f, 1f);
        accentRT.pivot    = new Vector2(0f, 0.5f);
        accentRT.anchoredPosition = Vector2.zero;
        accentRT.sizeDelta = new Vector2(4f, 0f);
        var accentImg = accentGO.AddComponent<Image>();
        accentImg.color = new Color(1f, 0.82f, 0.25f, 0.9f);
        accentImg.raycastTarget = false;
        accentGO.AddComponent<LayoutElement>().ignoreLayout = true;

        var layout = panelGO.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 18, 12, 14);
        layout.spacing = 5f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Panel height hugs however many rows are active
        var fitter = panelGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Quest name — bold amber gold, uppercase
        var title = CreateTextRow(panelGO.transform, "TitleText", 14, new Color(1f, 0.82f, 0.25f, 1f));
        title.fontStyle = FontStyles.Bold | FontStyles.UpperCase;

        // 1 px gold separator between title and objectives
        var sepGO = new GameObject("Separator");
        sepGO.transform.SetParent(panelGO.transform, false);
        var sepImg = sepGO.AddComponent<Image>();
        sepImg.color = new Color(1f, 0.82f, 0.25f, 0.28f);
        sepImg.raycastTarget = false;
        var sepLE = sepGO.AddComponent<LayoutElement>();
        sepLE.preferredHeight = 1f;

        // Objective text — warm off-white (turns gold on completion via SennaQuestHUD)
        var questText = CreateTextRow(panelGO.transform, "QuestText", 24, new Color(0.95f, 0.93f, 0.88f, 1f));

        var sideHeader = CreateTextRow(panelGO.transform, "SideHeader", 11, new Color(0.6f, 0.58f, 0.5f, 0.9f));
        sideHeader.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        sideHeader.text = "Side quests";

        var sideText = CreateTextRow(panelGO.transform, "SideQuestText", 18, new Color(0.75f, 0.73f, 0.68f, 0.9f));

        // --- Reward banner: center-screen slightly above crosshair, hidden by default ---
        DestroyIfExists(canvasGO.transform, "RewardBanner");
        var bannerGO = new GameObject("RewardBanner");
        Undo.RegisterCreatedObjectUndo(bannerGO, "Setup Quest HUD");
        bannerGO.transform.SetParent(canvasGO.transform, false);

        // Anchored to screen center, offset 90px up so it sits just above the crosshair
        var bannerRT = bannerGO.AddComponent<RectTransform>();
        bannerRT.anchorMin = bannerRT.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRT.pivot = new Vector2(0.5f, 0.5f);
        bannerRT.anchoredPosition = new Vector2(0, 90);
        bannerRT.sizeDelta = new Vector2(560, 0);

        // Dark parchment box — Zelda chest style
        var bannerBG = bannerGO.AddComponent<Image>();
        bannerBG.sprite = uiSprite;
        bannerBG.type = Image.Type.Sliced;
        bannerBG.color = new Color(0.07f, 0.06f, 0.04f, 0.96f);
        bannerBG.raycastTarget = false;

        var bannerLayout = bannerGO.AddComponent<VerticalLayoutGroup>();
        bannerLayout.padding = new RectOffset(28, 28, 14, 18);
        bannerLayout.spacing = 4f;
        bannerLayout.childControlWidth = true;
        bannerLayout.childControlHeight = true;
        bannerLayout.childForceExpandWidth = true;
        bannerLayout.childForceExpandHeight = false;

        var bannerFitter = bannerGO.AddComponent<ContentSizeFitter>();
        bannerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Row 1: "YOU GOT" — small, muted gold label
        var bannerTitleText = CreateTextRow(bannerGO.transform, "BannerTitle", 16, new Color(0.85f, 0.72f, 0.3f, 0.9f));
        bannerTitleText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        bannerTitleText.alignment = TextAlignmentOptions.Center;

        // Row 2: item name — big, bright gold, Zelda-style prominent
        var bannerBodyText = CreateTextRow(bannerGO.transform, "BannerBody", 32, new Color(1f, 0.9f, 0.35f, 1f));
        bannerBodyText.fontStyle = FontStyles.Bold;
        bannerBodyText.alignment = TextAlignmentOptions.Center;

        bannerGO.SetActive(false);

        var hud = panelGO.AddComponent<SennaQuestHUD>();
        var so = new SerializedObject(hud);
        so.FindProperty("background").objectReferenceValue = bg;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("questText").objectReferenceValue = questText;
        so.FindProperty("sideHeader").objectReferenceValue = sideHeader.gameObject;
        so.FindProperty("sideQuestText").objectReferenceValue = sideText;
        so.FindProperty("rewardBannerRoot").objectReferenceValue = bannerGO;
        so.FindProperty("rewardBannerTitle").objectReferenceValue = bannerTitleText;
        so.FindProperty("rewardBanner").objectReferenceValue = bannerBodyText;
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
