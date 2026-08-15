using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SciFiSettingsRowBuilder
{
    private const string SliderPrefabPath = "Assets/UI/Prefabs/Settings/SciFiSlider.prefab";
    private const string FontAssetPath =
        "Assets/Fonts/orbito-font/OrbitoRegular-7ODqA SDF.asset";
    private const string RowPrefabPath = "Assets/UI/Prefabs/Settings/SciFiSettingsRow.prefab";
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string SettingsContentPath =
        "SettingsPanel/SettingsWindow/SettingsContent";

    private readonly struct RowDefinition
    {
        public RowDefinition(string objectName, string displayName, float initialValue)
        {
            ObjectName = objectName;
            DisplayName = displayName;
            InitialValue = initialValue;
        }

        public string ObjectName { get; }
        public string DisplayName { get; }
        public float InitialValue { get; }
    }

    private static readonly RowDefinition[] Rows =
    {
        new RowDefinition("MasterVolumeRow", "MASTER VOLUME", 1f),
        new RowDefinition("MusicVolumeRow", "MUSIC VOLUME", 0.8f),
        new RowDefinition("SfxVolumeRow", "SFX VOLUME", 1f),
        new RowDefinition("BrightnessRow", "BRIGHTNESS", 1f)
    };

    [MenuItem("Tools/UI/Create Sci-Fi Settings Rows")]
    public static void BuildFromMenu()
    {
        BuildAndApply(selectCreatedAsset: true);
    }

    private static void BuildAndApply(bool selectCreatedAsset)
    {
        GameObject sliderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SliderPrefabPath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

        if (sliderPrefab == null || font == null)
        {
            throw new MissingReferenceException(
                "Cannot build SciFiSettingsRow: SciFiSlider prefab or Orbito font is missing."
            );
        }

        GameObject rowObject = CreateRowHierarchy(sliderPrefab, font);

        try
        {
            GameObject rowPrefab = PrefabUtility.SaveAsPrefabAsset(rowObject, RowPrefabPath);

            if (rowPrefab == null)
            {
                throw new UnityException($"Failed to create '{RowPrefabPath}'.");
            }

            ApplyRowsToMainMenu(rowPrefab);
            AssetDatabase.SaveAssets();

            if (selectCreatedAsset)
            {
                Selection.activeObject = rowPrefab;
                EditorGUIUtility.PingObject(rowPrefab);
            }

            Debug.Log($"Created and applied Sci-Fi settings rows: '{RowPrefabPath}'.", rowPrefab);
        }
        finally
        {
            Object.DestroyImmediate(rowObject);
        }
    }

    private static GameObject CreateRowHierarchy(GameObject sliderPrefab, TMP_FontAsset font)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        GameObject root = new GameObject(
            "SciFiSettingsRow",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement),
            typeof(SettingsSliderRowView)
        );
        root.layer = uiLayer >= 0 ? uiLayer : 5;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(740f, 52f);

        HorizontalLayoutGroup group = root.GetComponent<HorizontalLayoutGroup>();
        group.childAlignment = TextAnchor.MiddleLeft;
        group.spacing = 16f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        LayoutElement rowLayout = root.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = 52f;
        rowLayout.flexibleWidth = 1f;

        TextMeshProUGUI label = CreateText("Label", root.transform, root.layer, font);
        label.text = "MASTER VOLUME";
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = new Color32(213, 250, 255, 255);
        ConfigureLayout(label.gameObject, 190f, 52f, 0f);

        GameObject sliderObject = (GameObject)PrefabUtility.InstantiatePrefab(
            sliderPrefab,
            root.transform
        );
        sliderObject.name = "Slider";
        sliderObject.layer = root.layer;
        LayoutElement sliderLayout = sliderObject.GetComponent<LayoutElement>();
        sliderLayout.preferredWidth = 432f;
        sliderLayout.preferredHeight = 44f;
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.flexibleHeight = 0f;

        TextMeshProUGUI value = CreateText("ValueText", root.transform, root.layer, font);
        value.text = "100%";
        value.fontSize = 22f;
        value.fontStyle = FontStyles.Bold;
        value.alignment = TextAlignmentOptions.MidlineRight;
        value.color = new Color32(124, 250, 255, 255);
        ConfigureLayout(value.gameObject, 70f, 52f, 0f);

        SettingsSliderRowView rowView = root.GetComponent<SettingsSliderRowView>();
        rowView.Configure(label, sliderObject.GetComponent<Slider>(), value, label.text, 1f);

        return root;
    }

    private static void ApplyRowsToMainMenu(GameObject rowPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        GameObject canvas = GameObject.Find("MainMenuCanvas");

        if (canvas == null)
        {
            throw new MissingReferenceException("MainMenuCanvas was not found in Main Menu.");
        }

        Transform content = canvas.transform.Find(SettingsContentPath);

        if (content == null)
        {
            throw new MissingReferenceException(
                $"Main Menu hierarchy is missing '{SettingsContentPath}'."
            );
        }

        foreach (RowDefinition definition in Rows)
        {
            Transform existing = content.Find(definition.ObjectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        for (int index = 0; index < Rows.Length; index++)
        {
            RowDefinition definition = Rows[index];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, scene);
            instance.name = definition.ObjectName;
            instance.transform.SetParent(content, false);
            instance.transform.SetSiblingIndex(index);

            TMP_Text label = instance.transform.Find("Label").GetComponent<TMP_Text>();
            Slider slider = instance.transform.Find("Slider").GetComponent<Slider>();
            TMP_Text value = instance.transform.Find("ValueText").GetComponent<TMP_Text>();
            instance.GetComponent<SettingsSliderRowView>().Configure(
                label,
                slider,
                value,
                definition.DisplayName,
                definition.InitialValue
            );
        }

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 8, 8);
        contentLayout.spacing = 8f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        SetExistingRowHeight(content, "FullscreenRow", 52f);
        SetExistingRowHeight(content, "ResolutionRow", 52f);
        SetExistingRowHeight(content, "QualityRow", 52f);

        EditorSceneManager.MarkSceneDirty(scene);

        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new UnityException($"Failed to save '{MainMenuScenePath}'.");
        }
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        int layer,
        TMP_FontAsset font
    )
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement)
        );
        textObject.layer = layer;
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigureLayout(
        GameObject target,
        float preferredWidth,
        float preferredHeight,
        float flexibleWidth
    )
    {
        LayoutElement layout = target.GetComponent<LayoutElement>();
        layout.preferredWidth = preferredWidth;
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = flexibleWidth;
        layout.flexibleHeight = 0f;
    }

    private static void SetExistingRowHeight(Transform content, string rowName, float height)
    {
        Transform row = content.Find(rowName);
        if (row == null)
        {
            return;
        }

        LayoutElement layout = row.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.preferredHeight = height;
        }
    }
}
