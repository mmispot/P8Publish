using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WeaponCameraSetup : EditorWindow
{
    private Camera _mainCamera;
    private GameObject _armsObject;
    private float _weaponFov = 60f;

    [MenuItem("Senna/Setup Weapon Camera")]
    public static void ShowWindow()
    {
        GetWindow<WeaponCameraSetup>("Weapon Camera Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Weapon Camera Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Creates a dedicated overlay camera for the arms so they never clip into geometry.\n\n" +
            "1. Assign your main camera\n" +
            "2. Assign your arms/weapon GameObject\n" +
            "3. Click Setup",
            MessageType.Info);

        _mainCamera  = (Camera)EditorGUILayout.ObjectField("Main Camera", _mainCamera, typeof(Camera), true);
        _armsObject  = (GameObject)EditorGUILayout.ObjectField("Arms / Weapon Object", _armsObject, typeof(GameObject), true);
        _weaponFov   = EditorGUILayout.Slider("Weapon FOV", _weaponFov, 40f, 90f);

        EditorGUILayout.HelpBox(
            "Weapon FOV is intentionally lower than world FOV — 55–65 is standard. " +
            "This keeps arms feeling grounded regardless of sprint/ADS FOV changes on the main camera.",
            MessageType.None);

        EditorGUILayout.Space();

        GUI.enabled = _mainCamera != null && _armsObject != null;
        if (GUILayout.Button("Setup Weapon Camera", GUILayout.Height(30)))
            Run();
        GUI.enabled = true;
    }

    private void Run()
    {
        // 1 — Create or find the Weapon layer
        int weaponLayer = GetOrCreateLayer("Weapon");
        if (weaponLayer == -1)
        {
            Debug.LogError("WeaponCameraSetup: Could not create Weapon layer — all 32 layers are in use.");
            return;
        }

        // 2 — Assign arms and all children to Weapon layer, disable shadow casting
        SetLayerRecursive(_armsObject, weaponLayer);
        DisableShadowsRecursive(_armsObject);
        EditorUtility.SetDirty(_armsObject);

        // 3 — Main camera: exclude Weapon layer from culling mask
        _mainCamera.cullingMask &= ~(1 << weaponLayer);
        EditorUtility.SetDirty(_mainCamera);

        // 4 — Create weapon camera as child of main camera
        const string camName = "WeaponCamera";
        Transform existing = _mainCamera.transform.Find(camName);
        GameObject weaponCamGO = existing != null ? existing.gameObject : new GameObject(camName);
        weaponCamGO.transform.SetParent(_mainCamera.transform);
        weaponCamGO.transform.localPosition = Vector3.zero;
        weaponCamGO.transform.localRotation = Quaternion.identity;
        weaponCamGO.layer = _mainCamera.gameObject.layer;

        Camera weaponCam = weaponCamGO.GetComponent<Camera>() ?? weaponCamGO.AddComponent<Camera>();
        weaponCam.cullingMask  = 1 << weaponLayer;
        weaponCam.clearFlags   = CameraClearFlags.Depth;
        weaponCam.nearClipPlane = 0.01f;
        weaponCam.farClipPlane  = _mainCamera.farClipPlane;
        weaponCam.fieldOfView   = _weaponFov;
        weaponCam.depth         = _mainCamera.depth + 1;

        // 5 — Configure as URP Overlay and add to main camera stack
        var weaponCamData = weaponCamGO.GetComponent<UniversalAdditionalCameraData>()
                            ?? weaponCamGO.AddComponent<UniversalAdditionalCameraData>();
        weaponCamData.renderType = CameraRenderType.Overlay;

        var mainCamData = _mainCamera.GetComponent<UniversalAdditionalCameraData>();
        if (mainCamData != null)
        {
            if (!mainCamData.cameraStack.Contains(weaponCam))
                mainCamData.cameraStack.Add(weaponCam);
        }
        else
        {
            Debug.LogWarning("WeaponCameraSetup: Main camera has no UniversalAdditionalCameraData — are you on URP?");
        }

        EditorUtility.SetDirty(weaponCamGO);
        Debug.Log($"WeaponCameraSetup: Done! Arms on Weapon layer, weapon camera FOV {_weaponFov}°, shadows disabled on arms.");
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private static void DisableShadowsRecursive(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows    = false;
            EditorUtility.SetDirty(r);
        }
    }

    private static int GetOrCreateLayer(string layerName)
    {
        for (int i = 0; i < 32; i++)
            if (LayerMask.LayerToName(i) == layerName) return i;

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
        var layers = tagManager.FindProperty("layers");

        for (int i = 8; i < 32; i++)
        {
            var layerProp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerProp.stringValue))
            {
                layerProp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        return -1;
    }
}
