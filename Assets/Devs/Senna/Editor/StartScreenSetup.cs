using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public static class StartScreenSetup
{
    [MenuItem("Tools/Senna/Setup Start Screen UI")]
    public static void SetupUI()
    {
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

        // Start panel
        var startPanel = CreatePanel(canvasGO, "StartPanel", new Color(0f, 0f, 0f, 0.95f));
        CreateTMP(startPanel, "TitleText", "GAME TITLE", 72, new Vector2(0, 80));
        var startBtn = CreateButton(startPanel, "StartButton", "START", new Vector2(0, -40));
        UnityEventTools.AddPersistentListener(startBtn.onClick, gsm.OnStartPressed);

        // Pause panel
        var pausePanel = CreatePanel(canvasGO, "PausePanel", new Color(0f, 0f, 0f, 0.70f));
        CreateTMP(pausePanel, "PausedText", "PAUSED", 64, new Vector2(0, 100));
        var resumeBtn   = CreateButton(pausePanel, "ResumeButton",   "RESUME",    new Vector2(0,  40));
        var mainMenuBtn = CreateButton(pausePanel, "MainMenuButton", "MAIN MENU", new Vector2(0, -40));
        var quitBtn     = CreateButton(pausePanel, "QuitButton",     "QUIT",      new Vector2(0, -120));
        UnityEventTools.AddPersistentListener(resumeBtn.onClick,   gsm.OnResumePressed);
        UnityEventTools.AddPersistentListener(mainMenuBtn.onClick, gsm.OnMainMenuPressed);
        UnityEventTools.AddPersistentListener(quitBtn.onClick,     gsm.OnQuitPressed);

        // Wire refs
        var so = new SerializedObject(gsm);
        so.FindProperty("startPanel").objectReferenceValue = startPanel;
        so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        so.ApplyModifiedProperties();

        pausePanel.SetActive(false);

        // EventSystem is required for button clicks — create one if the scene doesn't have it
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Setup Start Screen UI");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        // Mark scene dirty so Unity saves all the wired references
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[StartScreenSetup] Done.");
    }

    static GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    static void CreateTMP(GameObject parent, string name, string text, float size, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900, 120);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }

    static Button CreateButton(GameObject parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 60);
        go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
        var btn = go.AddComponent<Button>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }
}
