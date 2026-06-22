using UnityEngine;
using UnityEditor;
using TMPro;

// Builds the ammo readout into UICanvas and makes sure the gun actually tracks ammo.
// Mirrors HealthHUDSetup: idempotent, leaves an existing AmmoHUD alone (delete + rerun to rebuild).
public static class AmmoHUDSetup
{
    [MenuItem("Tools/Senna/Setup Ammo HUD")]
    public static void SetupAmmoHUD()
    {
        var canvasGO = GameObject.Find("UICanvas");
        if (canvasGO == null)
        {
            var anyCanvas = Object.FindFirstObjectByType<Canvas>();
            canvasGO = anyCanvas != null ? anyCanvas.gameObject : null;
        }
        if (canvasGO == null)
        {
            Debug.LogError("[AmmoHUDSetup] No UICanvas found in the open scene. Run Tools > Senna > Setup Start Screen UI first.");
            return;
        }

        // Make sure the player has a SennaAmmoSystem and that the shooter + reload reference it,
        // otherwise firing never consumes rounds and the HUD has nothing to show.
        var ammo = EnsureAmmoSystem();

        var existing = canvasGO.transform.Find("AmmoHUD");
        if (existing != null)
        {
            Debug.Log("[AmmoHUDSetup] AmmoHUD already exists — leaving it as is. Delete it and rerun to rebuild.");
        }
        else
        {
            var ammoGO = new GameObject("AmmoHUD");
            Undo.RegisterCreatedObjectUndo(ammoGO, "Setup Ammo HUD");
            ammoGO.transform.SetParent(canvasGO.transform, false);
            ammoGO.transform.SetSiblingIndex(0); // behind the menu panels, like the health bar

            var rt = ammoGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f); // bottom-right
            rt.anchoredPosition = new Vector2(-40f, 40f);
            rt.sizeDelta = new Vector2(240f, 64f);

            var text = ammoGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 40f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.BottomRight;
            text.color = new Color(0.95f, 0.93f, 0.88f, 1f);
            text.raycastTarget = false;
            text.text = ammo != null ? $"{ammo.CurrentInMag} / {ammo.ReserveAmmo}" : "12 / 60";

            var hud = ammoGO.AddComponent<SennaAmmoHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("ammo").objectReferenceValue = ammo;
            so.FindProperty("ammoText").objectReferenceValue = text;
            so.ApplyModifiedProperties();
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[AmmoHUDSetup] Done. If UICanvas is a prefab instance, apply the overrides so AmmoHUD lands in UICanvas.prefab.");
    }

    // Finds the player's SennaAmmoSystem (adds one to the player root if missing) and wires
    // SchootingRaycast.ammo + SennaGunReload.ammo when they aren't already set.
    private static SennaAmmoSystem EnsureAmmoSystem()
    {
        var shooter = Object.FindFirstObjectByType<SchootingRaycast>(FindObjectsInactive.Include);
        var ammo = Object.FindFirstObjectByType<SennaAmmoSystem>(FindObjectsInactive.Include);

        if (ammo == null)
        {
            var host = shooter != null ? shooter.transform.root.gameObject : null;
            if (host == null)
            {
                Debug.LogWarning("[AmmoHUDSetup] No SchootingRaycast in the scene — couldn't place a SennaAmmoSystem. " +
                                 "Add one to the player and rerun.");
                return null;
            }
            ammo = Undo.AddComponent<SennaAmmoSystem>(host);
            Debug.Log($"[AmmoHUDSetup] Added a SennaAmmoSystem to '{host.name}'.");
        }

        WireAmmoReference(shooter, ammo);
        WireAmmoReference(Object.FindFirstObjectByType<SennaGunReload>(FindObjectsInactive.Include), ammo);
        return ammo;
    }

    private static void WireAmmoReference(Object target, SennaAmmoSystem ammo)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var p = so.FindProperty("ammo");
        if (p != null && p.objectReferenceValue == null)
        {
            p.objectReferenceValue = ammo;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
