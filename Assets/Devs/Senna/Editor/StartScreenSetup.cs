using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class StartScreenSetup
{
    const string PNG_PATH = "Assets/Devs/Senna/png for StartScreen/";

    [MenuItem("Tools/Senna/Setup Start Screen UI")]
    public static void SetupUI()
    {
        // Load sprites (reimports as Sprite type if needed)
        var bgSprite           = EnsureSprite(PNG_PATH + "tijdelijke_achtergrond.png");
        var startNormal        = EnsureSprite(PNG_PATH + "start_playing_no_border.png");
        var startHover         = EnsureSprite(PNG_PATH + "start_playing_with_border.png");
        var continueNormal     = EnsureSprite(PNG_PATH + "continue_no_border.png");
        var continueHover      = EnsureSprite(PNG_PATH + "continue_with_border.png");
        var settingsNormal     = EnsureSprite(PNG_PATH + "settings_no_border.png");
        var settingsHover      = EnsureSprite(PNG_PATH + "settings_with_border.png");
        var creditsNormal      = EnsureSprite(PNG_PATH + "credits_no_border.png");
        var creditsHover       = EnsureSprite(PNG_PATH + "credits_with_border.png");

        var gsmGO = new GameObject("GameStateManager");
        Undo.RegisterCreatedObjectUndo(gsmGO, "Setup Start Screen UI");
        var gsm = gsmGO.AddComponent<GameStateManager>();

        var canvasGO = new GameObject("UICanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Setup Start Screen UI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Start panel — full-screen background image
        var startPanel = CreatePanel(canvasGO, "StartPanel", bgSprite);
        var startBtn    = CreateButton(startPanel, "StartPlayingButton", startNormal,   startHover,   new Vector2(0,  100));
        var continueBtn = CreateButton(startPanel, "ContinueButton",     continueNormal, continueHover, new Vector2(0,   10));
        var settingsBtn = CreateButton(startPanel, "SettingsButton",     settingsNormal, settingsHover, new Vector2(0,  -80));
        var creditsBtn  = CreateButton(startPanel, "CreditsButton",      creditsNormal,  creditsHover,  new Vector2(0, -170));
        UnityEventTools.AddPersistentListener(startBtn.onClick,    gsm.OnStartPressed);
        UnityEventTools.AddPersistentListener(continueBtn.onClick, gsm.OnContinuePressed);
        UnityEventTools.AddPersistentListener(settingsBtn.onClick, gsm.OnSettingsPressed);
        UnityEventTools.AddPersistentListener(creditsBtn.onClick,  gsm.OnCreditsPressed);

        // Pause panel sprites
        var pauseBgSprite       = EnsureSprite(PNG_PATH + "pause_screen_background.png");
        var pausedSprite        = EnsureSprite(PNG_PATH + "paused.png");
        var pauseContinueNormal = EnsureSprite(PNG_PATH + "continue_no_border.png");
        var pauseContinueHover  = EnsureSprite(PNG_PATH + "continue_with_border.png");
        var menuNormal          = EnsureSprite(PNG_PATH + "menu_no_border.png");
        var menuHover           = EnsureSprite(PNG_PATH + "menu_with_border.png");
        var stopNormal          = EnsureSprite(PNG_PATH + "stop_playing_no_border.png");
        var stopHover           = EnsureSprite(PNG_PATH + "stop_playing_with_border.png");

        // Pause panel — background image + paused title sized to its natural aspect ratio
        var pausePanel  = CreatePanel(canvasGO, "PausePanel", pauseBgSprite);
        CreateImage(pausePanel, "PausedTitle", pausedSprite, new Vector2(0, 250), NativeSize(pausedSprite, 1400, 300));
        var pauseContinueBtn = CreateButton(pausePanel, "ContinueButton",    pauseContinueNormal, pauseContinueHover, new Vector2(0,  60));
        var mainMenuBtn      = CreateButton(pausePanel, "MenuButton",        menuNormal,          menuHover,          new Vector2(0, -40));
        var stopBtn          = CreateButton(pausePanel, "StopPlayingButton", stopNormal,          stopHover,          new Vector2(0, -140));
        UnityEventTools.AddPersistentListener(pauseContinueBtn.onClick, gsm.OnResumePressed);
        UnityEventTools.AddPersistentListener(mainMenuBtn.onClick,      gsm.OnMainMenuPressed);
        UnityEventTools.AddPersistentListener(stopBtn.onClick,          gsm.OnStopPlayingPressed);

        // Confirm popup sprites
        var areYouSureSprite = EnsureSprite(PNG_PATH + "are_you_sure.png");
        var yesNormal        = EnsureSprite(PNG_PATH + "yes_no_border.png");
        var yesHover         = EnsureSprite(PNG_PATH + "yes_with_border.png");
        var noNormal         = EnsureSprite(PNG_PATH + "no_no_border.png");
        var noHover          = EnsureSprite(PNG_PATH + "no_with_border.png");

        // Confirm panel — full-screen dimmer with centered popup sized to the sprite
        var confirmPanel = CreatePanel(canvasGO, "ConfirmPanel", null);
        SetPanelColor(confirmPanel, new Color(0f, 0f, 0f, 0.6f));
        var popup = CreatePopup(confirmPanel, "ConfirmPopup", areYouSureSprite, NativeSize(areYouSureSprite, 800, 600));
        var yesBtn = CreateButton(popup, "YesButton", yesNormal, yesHover, new Vector2(-130, -120));
        var noBtn  = CreateButton(popup, "NoButton",  noNormal,  noHover,  new Vector2( 130, -120));
        UnityEventTools.AddPersistentListener(yesBtn.onClick, gsm.OnConfirmYesPressed);
        UnityEventTools.AddPersistentListener(noBtn.onClick,  gsm.OnConfirmNoPressed);

        // Wire refs
        var so = new SerializedObject(gsm);
        so.FindProperty("startPanel").objectReferenceValue   = startPanel;
        so.FindProperty("pausePanel").objectReferenceValue   = pausePanel;
        so.FindProperty("confirmPanel").objectReferenceValue = confirmPanel;
        so.ApplyModifiedProperties();

        pausePanel.SetActive(false);
        confirmPanel.SetActive(false);

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Setup Start Screen UI");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[StartScreenSetup] Done.");
    }

    // Ensures the texture at assetPath is imported as Sprite type, then loads and returns it.
    static Sprite EnsureSprite(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static GameObject CreatePanel(GameObject parent, string name, Sprite bgSprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        if (bgSprite != null)
        {
            img.sprite = bgSprite;
            img.color  = Color.white;
            img.type   = Image.Type.Simple;
        }
        return go;
    }

    static void SetPanelColor(GameObject panel, Color color)
    {
        var img = panel.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    static void CreateTMP(GameObject parent, string name, string text, float size, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900, 120);
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }

    static void CreateImage(GameObject parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color  = Color.white;
        img.type   = Image.Type.Simple;
        img.preserveAspect = true;
    }

    // Scale sprite to fit within maxW×maxH while keeping its aspect ratio.
    static Vector2 NativeSize(Sprite s, float maxW, float maxH)
    {
        if (s == null) return new Vector2(maxW, maxH);
        float scale = Mathf.Min(maxW / s.rect.width, maxH / s.rect.height);
        return new Vector2(s.rect.width * scale, s.rect.height * scale);
    }

    static GameObject CreatePopup(GameObject parent, string name, Sprite bgSprite, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.sprite = bgSprite;
        img.color  = Color.white;
        img.type   = Image.Type.Simple;
        return go;
    }

    // withLabel = true keeps a TextMeshPro child (for pause-panel plain buttons)
    static Button CreateButton(GameObject parent, string name, Sprite normal, Sprite hover, Vector2 pos, bool withLabel = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 80);

        var img = go.AddComponent<Image>();
        if (normal != null)
        {
            img.sprite = normal;
            img.color  = Color.white;
        }
        else
        {
            img.color = new Color(1f, 1f, 1f, 0.15f);
        }

        var btn = go.AddComponent<Button>();
        if (hover != null)
        {
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState { highlightedSprite = hover };
        }

        if (withLabel)
        {
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;
            var tmp = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.fontSize  = 28;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color     = Color.white;
        }

        return btn;
    }

}
