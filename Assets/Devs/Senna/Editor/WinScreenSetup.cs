using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;

// Creates a WinPanel on the scene's UICanvas and wires it to GameStateManager.
// Run: Tools > Senna > Setup Win Screen
public static class WinScreenSetup
{
    const string PNG_PATH = "Assets/Devs/Senna/png for StartScreen/";

    [MenuItem("Tools/Senna/Setup Win Screen")]
    public static void SetupWinScreen()
    {
        var gsm = Object.FindFirstObjectByType<GameStateManager>(FindObjectsInactive.Include);
        if (gsm == null)
        {
            Debug.LogError("[WinScreenSetup] No GameStateManager in the scene. Open MainScene first.");
            return;
        }

        var canvasGO = GameObject.Find("UICanvas");
        if (canvasGO == null)
        {
            Debug.LogError("[WinScreenSetup] No UICanvas found. Run 'Tools > Senna > Setup Start Screen UI' first.");
            return;
        }

        // Guard: don't create a second WinPanel
        var existing = canvasGO.transform.Find("WinPanel");
        if (existing != null)
        {
            Debug.Log("[WinScreenSetup] WinPanel already exists — nothing to do.");
            return;
        }

        // Reuse the same background as the death screen, but tint it golden (no red).
        var bgSprite      = EnsureSprite(PNG_PATH + "death_screen_background.png");
        var menuNormal    = EnsureSprite(PNG_PATH + "menu_no_border.png");
        var menuHover     = EnsureSprite(PNG_PATH + "menu_with_border.png");
        var playNormal    = EnsureSprite(PNG_PATH + "start_playing_no_border.png");
        var playHover     = EnsureSprite(PNG_PATH + "start_playing_with_border.png");

        // Panel — same full-screen layout as the death panel
        var winPanel = CreatePanel(canvasGO, "WinPanel", bgSprite, new Color(0.85f, 0.75f, 0.2f, 1f));
        Undo.RegisterCreatedObjectUndo(winPanel, "Setup Win Screen");

        // "You Win!" title
        CreateTMP(winPanel, "WinTitle", "YOU WIN!", 96, new Vector2(0, 260));

        // Buttons
        var menuBtn = CreateButton(winPanel, "MainMenuButton",  menuNormal, menuHover,  new Vector2(0,  60));
        var playBtn = CreateButton(winPanel, "PlayAgainButton", playNormal, playHover,  new Vector2(0, -50));

        UnityEventTools.AddPersistentListener(menuBtn.onClick, gsm.OnWinMainMenuPressed);
        UnityEventTools.AddPersistentListener(playBtn.onClick, gsm.OnWinPlayAgainPressed);

        // Wire the winPanel ref into GameStateManager
        var so = new SerializedObject(gsm);
        so.FindProperty("winPanel").objectReferenceValue = winPanel;
        so.ApplyModifiedProperties();

        winPanel.SetActive(false);

        EditorUtility.SetDirty(gsm);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[WinScreenSetup] WinPanel created and wired. Save the scene (Ctrl+S).");
    }

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

    static GameObject CreatePanel(GameObject parent, string name, Sprite bgSprite, Color tint)
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
            img.type   = Image.Type.Simple;
        }
        img.color = tint;
        return go;
    }

    static void CreateTMP(GameObject parent, string name, string text, float size, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900, 140);
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color     = Color.white;
    }

    static Button CreateButton(GameObject parent, string name, Sprite normal, Sprite hover, Vector2 pos)
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
            btn.transition  = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState { highlightedSprite = hover };
        }
        return btn;
    }
}
