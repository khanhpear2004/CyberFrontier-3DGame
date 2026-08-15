using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SciFiSliderBuilder
{
    private const string TrackSpritePath =
        "Assets/UIAssets/Assets Wenrexa UI Sci-Fi Minimalism #01/СommonElement/ScrollBar.png";
    private const string HandleSpritePath =
        "Assets/UIAssets/Assets Wenrexa UI Sci-Fi Minimalism #01/СommonElement/Slider.png";
    private const string PrefabFolder = "Assets/UI/Prefabs/Settings";
    private const string PrefabPath = PrefabFolder + "/SciFiSlider.prefab";
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string SliderParentPath =
        "SettingsPanel/SettingsWindow/SettingsContent/MasterVolumeRow";

    [MenuItem("Tools/UI/Create Sci-Fi Settings Slider Prefab")]
    public static void BuildFromMenu()
    {
        BuildPrefabAndApplyToMainMenu(selectCreatedAsset: true);
    }

    public static void BuildFromCommandLine()
    {
        BuildPrefabAndApplyToMainMenu(selectCreatedAsset: false);
    }

    private static void BuildPrefabAndApplyToMainMenu(bool selectCreatedAsset)
    {
        Sprite trackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TrackSpritePath);
        Sprite handleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HandleSpritePath);

        if (trackSprite == null || handleSprite == null)
        {
            throw new MissingReferenceException(
                "Cannot build SciFiSlider: ScrollBar.png or Slider.png is not imported as a Sprite."
            );
        }

        EnsureFolder("Assets", "UI");
        EnsureFolder("Assets/UI", "Prefabs");
        EnsureFolder("Assets/UI/Prefabs", "Settings");

        GameObject sliderObject = CreateSliderHierarchy(trackSprite, handleSprite);

        try
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sliderObject, PrefabPath);

            if (prefab == null)
            {
                throw new UnityException($"Failed to create prefab at '{PrefabPath}'.");
            }

            ApplyPrefabToMainMenu(prefab);
            AssetDatabase.SaveAssets();

            if (selectCreatedAsset)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            Debug.Log($"Created and applied Sci-Fi settings slider: '{PrefabPath}'.", prefab);
        }
        finally
        {
            Object.DestroyImmediate(sliderObject);
        }
    }

    private static GameObject CreateSliderHierarchy(Sprite trackSprite, Sprite handleSprite)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        GameObject root = new GameObject(
            "SciFiSlider",
            typeof(RectTransform),
            typeof(Slider),
            typeof(LayoutElement)
        );
        root.layer = uiLayer >= 0 ? uiLayer : 5;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(390f, 44f);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.preferredWidth = 320f;
        layout.preferredHeight = 44f;
        layout.flexibleWidth = 1f;
        layout.flexibleHeight = 0f;

        Image background = CreateImage("Background", root.transform, root.layer);
        RectTransform backgroundRect = background.rectTransform;
        SetHorizontalBarRect(backgroundRect, 0f, 0f, 18f);
        background.sprite = trackSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color32(19, 36, 58, 255);
        background.raycastTarget = false;

        RectTransform fillArea = CreateRect("Fill Area", root.transform, root.layer);
        SetHorizontalBarRect(fillArea, 8f, -8f, 10f);

        Image fill = CreateImage("Fill", fillArea, root.layer);
        Stretch(fill.rectTransform);
        fill.sprite = null;
        fill.type = Image.Type.Simple;
        fill.color = new Color32(36, 223, 255, 255);
        fill.raycastTarget = false;

        RectTransform handleArea = CreateRect("Handle Slide Area", root.transform, root.layer);
        handleArea.anchorMin = new Vector2(0f, 0f);
        handleArea.anchorMax = new Vector2(1f, 1f);
        handleArea.offsetMin = new Vector2(15f, 0f);
        handleArea.offsetMax = new Vector2(-15f, 0f);

        Image handle = CreateImage("Handle", handleArea, root.layer);
        RectTransform handleRect = handle.rectTransform;
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(30f, 30f);
        handle.sprite = handleSprite;
        handle.type = Image.Type.Simple;
        handle.preserveAspect = true;
        handle.color = Color.white;
        handle.raycastTarget = true;

        Slider slider = root.GetComponent<Slider>();
        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(1f);
        slider.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = slider.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(124, 250, 255, 255);
        colors.pressedColor = Color.white;
        colors.selectedColor = new Color32(124, 250, 255, 255);
        colors.disabledColor = new Color32(88, 101, 116, 128);
        colors.colorMultiplier = 1.25f;
        colors.fadeDuration = 0.08f;
        slider.colors = colors;

        Navigation navigation = slider.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        slider.navigation = navigation;

        return root;
    }

    private static void ApplyPrefabToMainMenu(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        GameObject canvas = GameObject.Find("MainMenuCanvas");

        if (canvas == null)
        {
            throw new MissingReferenceException("MainMenuCanvas was not found in the Main Menu scene.");
        }

        Transform row = canvas.transform.Find(SliderParentPath);

        if (row == null)
        {
            throw new MissingReferenceException(
                $"Main Menu hierarchy is missing '{SliderParentPath}'."
            );
        }

        Transform oldSlider = row.Find("Slider");
        int siblingIndex = oldSlider != null ? oldSlider.GetSiblingIndex() : 1;

        if (oldSlider != null)
        {
            Object.DestroyImmediate(oldSlider.gameObject);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "Slider";
        instance.transform.SetParent(row, false);
        instance.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, row.childCount - 1));

        RectTransform instanceRect = instance.GetComponent<RectTransform>();
        instanceRect.localScale = Vector3.one;

        EditorSceneManager.MarkSceneDirty(scene);

        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new UnityException($"Failed to save '{MainMenuScenePath}'.");
        }
    }

    private static Image CreateImage(string name, Transform parent, int layer)
    {
        GameObject child = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return child.GetComponent<Image>();
    }

    private static RectTransform CreateRect(string name, Transform parent, int layer)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.layer = layer;
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void SetHorizontalBarRect(
        RectTransform rect,
        float left,
        float right,
        float height
    )
    {
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, -height * 0.5f);
        rect.offsetMax = new Vector2(right, height * 0.5f);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureFolder(string parentFolder, string childName)
    {
        string folderPath = parentFolder + "/" + childName;

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, childName);
        }
    }
}
