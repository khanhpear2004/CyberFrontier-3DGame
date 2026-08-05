using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuHierarchyBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string FontAssetPath = "Assets/Fonts/orbito-font/OrbitoRegular-7ODqA SDF.asset";

    private static readonly Color BackgroundColor = new Color(0.025f, 0.045f, 0.08f, 1f);
    private static readonly Color WindowColor = new Color(0.075f, 0.095f, 0.14f, 0.98f);
    private static readonly Color ControlColor = new Color(0.13f, 0.17f, 0.23f, 1f);
    private static readonly Color AccentColor = new Color(0.1f, 0.85f, 0.9f, 1f);
    private static readonly Color TextColor = new Color(0.94f, 0.98f, 1f, 1f);

    private static TMP_FontAsset menuFont;

    [MenuItem("Tools/UI/Build Main Menu Hierarchy")]
    public static void BuildMainMenuHierarchy()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (Application.isBatchMode && scene.path != MainMenuScenePath)
        {
            scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        if (scene.path != MainMenuScenePath)
        {
            EditorUtility.DisplayDialog(
                "Wrong Scene",
                $"Open '{MainMenuScenePath}' before running this tool. The current scene is '{scene.path}'.",
                "OK"
            );
            return;
        }

        if (GameObject.Find("MainMenuCanvas") != null)
        {
            bool rebuild = Application.isBatchMode || EditorUtility.DisplayDialog(
                    "Main Menu Already Exists",
                    "MainMenuCanvas already exists. Rebuilding will replace the generated Main Menu hierarchy.",
                    "Rebuild",
                    "Cancel"
                );

            if (!rebuild)
            {
                return;
            }

            Object.DestroyImmediate(GameObject.Find("MainMenuCanvas"));
        }

        GameObject existingEventSystem = GameObject.Find("EventSystem");
        if (existingEventSystem != null)
        {
            Object.DestroyImmediate(existingEventSystem);
        }

        GameObject existingSettingsManager = GameObject.Find("GameSettingsManager");
        if (existingSettingsManager != null)
        {
            Object.DestroyImmediate(existingSettingsManager);
        }

        menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Main Menu Hierarchy");

        CreateEventSystem();
        CreateSettingsManagerPlaceholder();
        CreateCanvasHierarchy();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = GameObject.Find("MainMenuCanvas");
        EditorGUIUtility.PingObject(Selection.activeGameObject);

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Main Menu Created",
                "The Main Menu hierarchy was created and the scene was saved. SettingsPanel and LoadingOverlay are inactive by default.",
                "OK"
            );
        }
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");

        InputSystemUIInputModule inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private static void CreateSettingsManagerPlaceholder()
    {
        GameObject settingsManager = new GameObject("GameSettingsManager");
        Undo.RegisterCreatedObjectUndo(settingsManager, "Create GameSettingsManager");
    }

    private static void CreateCanvasHierarchy()
    {
        GameObject canvasObject = new GameObject(
            "MainMenuCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create MainMenuCanvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        CreateBackground(canvasRect);
        CreateMainMenuRoot(canvasRect);
        CreateSettingsPanel(canvasRect);
        CreateLoadingOverlay(canvasRect);
    }

    private static void CreateBackground(Transform parent)
    {
        Image background = CreateImage("Background", parent, BackgroundColor);
        Stretch(background.rectTransform);
        background.raycastTarget = false;
    }

    private static void CreateMainMenuRoot(Transform parent)
    {
        RectTransform root = CreateRect("MainMenuRoot", parent);
        Stretch(root);

        TextMeshProUGUI title = CreateText("GameTitle", root, "CYBER FRONTIER", 72f, TextAlignmentOptions.Center, true);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(900f, 120f));
        title.color = AccentColor;

        RectTransform buttonContainer = CreateRect("MenuButtonContainer", root);
        SetRect(buttonContainer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(380f, 300f));

        VerticalLayoutGroup layout = buttonContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateButton("PlayButton", buttonContainer, "PLAY", 340f, 68f);
        CreateButton("SettingsButton", buttonContainer, "SETTINGS", 340f, 68f);
        CreateButton("QuitButton", buttonContainer, "QUIT", 340f, 68f);
    }

    private static void CreateSettingsPanel(Transform parent)
    {
        RectTransform panel = CreateRect("SettingsPanel", parent);
        Stretch(panel);

        Image dimBackground = CreateImage("DimBackground", panel, new Color(0f, 0f, 0f, 0.72f));
        Stretch(dimBackground.rectTransform);
        dimBackground.raycastTarget = true;

        Image window = CreateImage("SettingsWindow", panel, WindowColor);
        SetRect(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 700f));

        VerticalLayoutGroup windowLayout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(40, 40, 32, 32);
        windowLayout.spacing = 22f;
        windowLayout.childAlignment = TextAnchor.UpperCenter;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        RectTransform header = CreateRect("Header", window.transform);
        AddLayoutElement(header.gameObject, -1f, 76f, 0f, 0f);
        TextMeshProUGUI title = CreateText("Title", header, "SETTINGS", 42f, TextAlignmentOptions.Center, true);
        Stretch(title.rectTransform);
        title.color = AccentColor;

        RectTransform content = CreateRect("SettingsContent", window.transform);
        AddLayoutElement(content.gameObject, -1f, -1f, 1f, 1f);

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 18f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        CreateMasterVolumeRow(content);
        CreateDropdownRow("ResolutionRow", content, "Resolution", "ResolutionDropdown", "1920 x 1080");
        CreateFullscreenRow(content);
        CreateDropdownRow("QualityRow", content, "Quality", "QualityDropdown", "PC");

        RectTransform footer = CreateRect("Footer", window.transform);
        AddLayoutElement(footer.gameObject, -1f, 72f, 0f, 0f);

        HorizontalLayoutGroup footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 16f;
        footerLayout.childAlignment = TextAnchor.MiddleCenter;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = true;

        CreateButton("DefaultButton", footer, "DEFAULT", 200f, 58f);
        CreateButton("ApplyButton", footer, "APPLY", 200f, 58f);
        CreateButton("BackButton", footer, "BACK", 200f, 58f);

        panel.gameObject.SetActive(false);
    }

    private static void CreateMasterVolumeRow(Transform parent)
    {
        RectTransform row = CreateSettingsRow("MasterVolumeRow", parent);
        CreateRowLabel(row, "Master Volume");

        Slider slider = CreateSlider("Slider", row);
        AddLayoutElement(slider.gameObject, 320f, 44f, 1f, 0f);
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.wholeNumbers = false;

        TextMeshProUGUI valueText = CreateText("ValueText", row, "100%", 24f, TextAlignmentOptions.Center, false);
        AddLayoutElement(valueText.gameObject, 80f, 44f, 0f, 0f);
    }

    private static void CreateDropdownRow(string rowName, Transform parent, string labelText, string dropdownName, string defaultCaption)
    {
        RectTransform row = CreateSettingsRow(rowName, parent);
        CreateRowLabel(row, labelText);

        TMP_Dropdown dropdown = CreateDropdown(dropdownName, row, defaultCaption);
        AddLayoutElement(dropdown.gameObject, 420f, 52f, 1f, 0f);
    }

    private static void CreateFullscreenRow(Transform parent)
    {
        RectTransform row = CreateSettingsRow("FullscreenRow", parent);
        CreateRowLabel(row, "Fullscreen");

        Toggle toggle = CreateToggle("FullscreenToggle", row);
        AddLayoutElement(toggle.gameObject, 52f, 52f, 0f, 0f);

        RectTransform spacer = CreateRect("Spacer", row);
        AddLayoutElement(spacer.gameObject, -1f, 52f, 1f, 0f);
    }

    private static RectTransform CreateSettingsRow(string name, Transform parent)
    {
        RectTransform row = CreateRect(name, parent);
        AddLayoutElement(row.gameObject, -1f, 66f, 0f, 0f);

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        return row;
    }

    private static void CreateRowLabel(Transform parent, string text)
    {
        TextMeshProUGUI label = CreateText("Label", parent, text, 26f, TextAlignmentOptions.MidlineLeft, false);
        AddLayoutElement(label.gameObject, 230f, 52f, 0f, 0f);
    }

    private static void CreateLoadingOverlay(Transform parent)
    {
        Image overlay = CreateImage("LoadingOverlay", parent, new Color(0.015f, 0.025f, 0.045f, 1f));
        Stretch(overlay.rectTransform);

        TextMeshProUGUI loadingText = CreateText("LoadingText", overlay.transform, "LOADING...", 38f, TextAlignmentOptions.Center, true);
        SetRect(loadingText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 90f));
        loadingText.color = AccentColor;

        overlay.gameObject.SetActive(false);
    }

    private static Button CreateButton(string name, Transform parent, string labelText, float preferredWidth, float preferredHeight)
    {
        Image image = CreateImage(name, parent, ControlColor);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = ControlColor;
        colors.highlightedColor = new Color(0.16f, 0.3f, 0.36f, 1f);
        colors.pressedColor = new Color(0.08f, 0.7f, 0.75f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.1f, 0.12f, 0.15f, 0.55f);
        button.colors = colors;

        AddLayoutElement(button.gameObject, preferredWidth, preferredHeight, 0f, 0f);

        TextMeshProUGUI label = CreateText("Label", button.transform, labelText, 28f, TextAlignmentOptions.Center, true);
        Stretch(label.rectTransform, 14f, 14f, 8f, 8f);

        return button;
    }

    private static Slider CreateSlider(string name, Transform parent)
    {
        RectTransform root = CreateRect(name, parent);
        Slider slider = root.gameObject.AddComponent<Slider>();

        Image background = CreateImage("Background", root, new Color(0.05f, 0.07f, 0.1f, 1f));
        SetRect(background.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 12f));

        RectTransform fillArea = CreateRect("Fill Area", root);
        Stretch(fillArea, 8f, 18f, 8f, 8f);

        Image fill = CreateImage("Fill", fillArea, AccentColor);
        Stretch(fill.rectTransform);

        RectTransform handleArea = CreateRect("Handle Slide Area", root);
        Stretch(handleArea, 10f, 10f, 0f, 0f);

        Image handle = CreateImage("Handle", handleArea, TextColor);
        SetRect(handle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 34f));

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private static Toggle CreateToggle(string name, Transform parent)
    {
        RectTransform root = CreateRect(name, parent);
        Toggle toggle = root.gameObject.AddComponent<Toggle>();

        Image background = CreateImage("Background", root, ControlColor);
        Stretch(background.rectTransform, 5f, 5f, 5f, 5f);

        Image checkmark = CreateImage("Checkmark", background.transform, AccentColor);
        Stretch(checkmark.rectTransform, 9f, 9f, 9f, 9f);

        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        toggle.isOn = true;

        return toggle;
    }

    private static TMP_Dropdown CreateDropdown(string name, Transform parent, string defaultCaption)
    {
        Image rootImage = CreateImage(name, parent, ControlColor);
        TMP_Dropdown dropdown = rootImage.gameObject.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = rootImage;

        TextMeshProUGUI caption = CreateText("Label", rootImage.transform, defaultCaption, 23f, TextAlignmentOptions.MidlineLeft, false);
        Stretch(caption.rectTransform, 18f, 52f, 5f, 5f);

        TextMeshProUGUI arrow = CreateText("Arrow", rootImage.transform, "▼", 19f, TextAlignmentOptions.Center, false);
        SetRect(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(40f, 40f));

        Image templateImage = CreateImage("Template", rootImage.transform, WindowColor);
        RectTransform template = templateImage.rectTransform;
        SetRect(template, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(0f, 210f));

        ScrollRect scrollRect = templateImage.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        Image viewportImage = CreateImage("Viewport", template, new Color(0.04f, 0.06f, 0.09f, 1f));
        RectTransform viewport = viewportImage.rectTransform;
        Stretch(viewport, 4f, 4f, 4f, 4f);
        Mask mask = viewportImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 44f);

        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image itemBackground = CreateImage("Item", content, new Color(0.1f, 0.14f, 0.2f, 1f));
        AddLayoutElement(itemBackground.gameObject, -1f, 44f, 1f, 0f);
        Toggle itemToggle = itemBackground.gameObject.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemBackground;

        Image itemCheckmark = CreateImage("Item Checkmark", itemBackground.transform, AccentColor);
        SetRect(itemCheckmark.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f, 0f), new Vector2(16f, 16f));
        itemToggle.graphic = itemCheckmark;

        TextMeshProUGUI itemLabel = CreateText("Item Label", itemBackground.transform, "Option", 21f, TextAlignmentOptions.MidlineLeft, false);
        Stretch(itemLabel.rectTransform, 42f, 10f, 2f, 2f);

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        dropdown.template = template;
        dropdown.captionText = caption;
        dropdown.itemText = itemLabel;
        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData(defaultCaption));
        dropdown.RefreshShownValue();

        template.gameObject.SetActive(false);
        return dropdown;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        CanvasRenderer renderer = rect.gameObject.AddComponent<CanvasRenderer>();
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, bool bold)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.gameObject.AddComponent<CanvasRenderer>();

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = TextColor;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;

        if (menuFont != null)
        {
            label.font = menuFont;
        }

        return label;
    }

    private static LayoutElement AddLayoutElement(GameObject gameObject, float preferredWidth, float preferredHeight, float flexibleWidth, float flexibleHeight)
    {
        LayoutElement element = gameObject.AddComponent<LayoutElement>();

        if (preferredWidth >= 0f)
        {
            element.preferredWidth = preferredWidth;
        }

        if (preferredHeight >= 0f)
        {
            element.preferredHeight = preferredHeight;
        }

        element.flexibleWidth = flexibleWidth;
        element.flexibleHeight = flexibleHeight;
        return element;
    }

    private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }
}
