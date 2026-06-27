using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class HealthHUDSetup
{
    [MenuItem("Tools/Senna/Setup Health HUD")]
    public static void SetupHealthHUD()
    {
        var canvasGO = GameObject.Find("UICanvas");
        if (canvasGO == null)
        {
            var anyCanvas = Object.FindFirstObjectByType<Canvas>();
            canvasGO = anyCanvas != null ? anyCanvas.gameObject : null;
        }
        if (canvasGO == null)
        {
            Debug.LogError("[HealthHUDSetup] No UICanvas found in the open scene. Run Tools > Senna > Setup Start Screen UI first.");
            return;
        }

        var playerHealth = Object.FindFirstObjectByType<SennaPlayerHealth>(FindObjectsInactive.Include);
        if (playerHealth == null)
            Debug.LogWarning("[HealthHUDSetup] No SennaPlayerHealth in scene — wire Player Health manually on HealthBar and DamageFlash.");

        // Add SennaCameraShake to the camera automatically if it isn't there yet
        var cameraShake = Object.FindFirstObjectByType<SennaCameraShake>(FindObjectsInactive.Include);
        if (cameraShake == null)
        {
            var cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (cam != null)
            {
                cameraShake = Undo.AddComponent<SennaCameraShake>(cam.gameObject);
                Debug.Log($"[HealthHUDSetup] Added SennaCameraShake to {cam.gameObject.name}.");
            }
            else
            {
                Debug.LogWarning("[HealthHUDSetup] No camera found — add SennaCameraShake to your camera manually.");
            }
        }

        // Built-in placeholder sprite; real art can be swapped in on the Images later.
        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // --- Health bar (bottom-left), behind all menu panels ---
        var healthBarT = canvasGO.transform.Find("HealthBar");
        GameObject healthBarGO;
        if (healthBarT != null)
        {
            healthBarGO = healthBarT.gameObject;
            Debug.Log("[HealthHUDSetup] HealthBar already exists — leaving it as is.");
        }
        else
        {
            healthBarGO = new GameObject("HealthBar");
            Undo.RegisterCreatedObjectUndo(healthBarGO, "Setup Health HUD");
            healthBarGO.transform.SetParent(canvasGO.transform, false);
            healthBarGO.transform.SetSiblingIndex(0);

            var rt = healthBarGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(40, 40);
            rt.sizeDelta = new Vector2(300, 40);

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(healthBarGO.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite = uiSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            bgImg.raycastTarget = false;

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(healthBarGO.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(4, 4);
            fillRT.offsetMax = new Vector2(-4, -4);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.sprite = uiSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;
            fillImg.color = new Color(0.85f, 0.15f, 0.15f, 1f);
            fillImg.raycastTarget = false;

            var barUI = healthBarGO.AddComponent<SennaHealthBarUI>();
            var so = new SerializedObject(barUI);
            so.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("fillImage").objectReferenceValue = fillImg;
            so.ApplyModifiedProperties();
        }

        // --- Full-screen damage flash, above the bar but behind menu panels ---
        var flashT = canvasGO.transform.Find("DamageFlash");
        if (flashT != null)
        {
            Debug.Log("[HealthHUDSetup] DamageFlash already exists — leaving it as is.");
        }
        else
        {
            var flashGO = new GameObject("DamageFlash");
            Undo.RegisterCreatedObjectUndo(flashGO, "Setup Health HUD");
            flashGO.transform.SetParent(canvasGO.transform, false);
            flashGO.transform.SetSiblingIndex(healthBarGO.transform.GetSiblingIndex() + 1);

            var rt = flashGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var img = flashGO.AddComponent<Image>();
            img.color = new Color(0.7f, 0f, 0f, 0f);
            img.raycastTarget = false;

            var feedback = flashGO.AddComponent<SennaDamageFeedback>();
            var so = new SerializedObject(feedback);
            so.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("cameraShake").objectReferenceValue = cameraShake;
            so.FindProperty("damageFlashImage").objectReferenceValue = img;
            so.ApplyModifiedProperties();
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[HealthHUDSetup] Done. If UICanvas is a prefab instance, apply the overrides so HealthBar and DamageFlash end up in UICanvas.prefab.");
    }

    [MenuItem("Tools/Senna/Setup Crosshair")]
    public static void SetupCrosshair()
    {
        var canvasGO = GameObject.Find("UICanvas");
        if (canvasGO == null)
        {
            var anyCanvas = Object.FindFirstObjectByType<Canvas>();
            canvasGO = anyCanvas != null ? anyCanvas.gameObject : null;
        }
        if (canvasGO == null)
        {
            Debug.LogError("[HealthHUDSetup] No UICanvas found in the open scene.");
            return;
        }

        if (canvasGO.transform.Find("Crosshair") != null)
        {
            Debug.Log("[HealthHUDSetup] Crosshair already exists — leaving it as is.");
            return;
        }

        // Knob is Unity's built-in round sprite — gives a circle dot out of the box
        var dotSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        var crosshairGO = new GameObject("Crosshair");
        Undo.RegisterCreatedObjectUndo(crosshairGO, "Setup Crosshair");
        crosshairGO.transform.SetParent(canvasGO.transform, false);

        var rt = crosshairGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(8f, 8f);

        var img = crosshairGO.AddComponent<Image>();
        img.sprite        = dotSprite;
        img.color         = new Color(1f, 1f, 1f, 0.9f);
        img.raycastTarget = false;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[HealthHUDSetup] Crosshair dot added. Apply UICanvas prefab overrides to save it.");
    }
}
