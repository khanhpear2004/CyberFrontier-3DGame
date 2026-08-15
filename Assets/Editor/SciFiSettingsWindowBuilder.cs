using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SciFiSettingsWindowBuilder
{
    private const string CommonFolder =
        "Assets/UIAssets/Assets Wenrexa UI Sci-Fi Minimalism #01/СommonElement/";
    private const string PanelSpritePath =
        "Assets/UIAssets/Assets Wenrexa UI Sci-Fi Minimalism #01/Elements/BlockInformation.png";
    private const string FontAssetPath =
        "Assets/Fonts/orbito-font/OrbitoRegular-7ODqA SDF.asset";
    private const string HoverMaterialPath = "Assets/UI/Materials/SciFiButtonHover.mat";
    private const string SecondaryButtonPath =
        "Assets/UI/Prefabs/Common/SciFiSecondaryButton.prefab";
    private const string BackButtonPath = "Assets/UI/Prefabs/Common/SciFiBackButton.prefab";
    private const string SettingsWindowPrefabPath =
        "Assets/UI/Prefabs/Settings/SciFiSettingsWindow.prefab";
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";

    [MenuItem("Tools/UI/Style Sci-Fi Settings Window")]
    public static void BuildFromMenu()
    {
        BuildAndApply(selectCreatedAsset: true);
    }

    private static void BuildAndApply(bool selectCreatedAsset)
    {
        Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
        Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CommonFolder + "BtnDefault.png");
        Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CommonFolder + "BtnBack.png");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Material hoverMaterial = AssetDatabase.LoadAssetAtPath<Material>(HoverMaterialPath);

        if (panelSprite == null || defaultSprite == null || backSprite == null ||
            font == null || hoverMaterial == null)
        {
            throw new MissingReferenceException(
                "Cannot style Settings: panel/button sprites, Orbito font, or hover material is missing."
            );
        }

        GameObject secondaryButton = CreateButton(
            "SciFiSecondaryButton",
            defaultSprite,
            font,
            hoverMaterial,
            "APPLY"
        );
        GameObject backButton = CreateButton(
            "SciFiBackButton",
            backSprite,
            font,
            hoverMaterial,
            "BACK"
        );

        try
        {
            GameObject secondaryPrefab = PrefabUtility.SaveAsPrefabAsset(
                secondaryButton,
                SecondaryButtonPath
            );
            GameObject backPrefab = PrefabUtility.SaveAsPrefabAsset(backButton, BackButtonPath);

            if (secondaryPrefab == null || backPrefab == null)
            {
                throw new UnityException("Failed to create Settings button prefabs.");
            }

            GameObject windowPrefab = ApplyToMainMenu(
                panelSprite,
                secondaryPrefab,
                backPrefab
            );
            AssetDatabase.SaveAssets();

            if (selectCreatedAsset)
            {
                Selection.activeObject = windowPrefab;
                EditorGUIUtility.PingObject(windowPrefab);
            }

            Debug.Log("Styled SettingsWindow at 80% viewport and created Settings button prefabs.");
        }
        finally
        {
            Object.DestroyImmediate(secondaryButton);
            Object.DestroyImmediate(backButton);
        }
    }

    private static GameObject CreateButton(
        string objectName,
        Sprite sprite,
        TMP_FontAsset font,
        Material hoverMaterial,
        string defaultLabel
    )
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        GameObject root = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );
        root.layer = uiLayer >= 0 ? uiLayer : 5;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(260f, 64f);

        Image background = root.GetComponent<Image>();
        background.sprite = sprite;
        background.type = Image.Type.Sliced;
        background.color = Color.white;
        background.raycastTarget = true;

        Button button = root.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        button.navigation = navigation;

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.preferredWidth = 260f;
        layout.preferredHeight = 64f;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        Image hoverOverlay = CreateImage("HoverOverlay", root.transform, root.layer);
        Stretch(hoverOverlay.rectTransform);
        hoverOverlay.sprite = sprite;
        hoverOverlay.type = Image.Type.Sliced;
        hoverOverlay.material = hoverMaterial;
        hoverOverlay.color = Color.clear;
        hoverOverlay.raycastTarget = false;

        TextMeshProUGUI label = CreateLabel(root.transform, root.layer, font, defaultLabel);

        SciFiButtonHoverFeedback feedback = root.AddComponent<SciFiButtonHoverFeedback>();
        feedback.Configure(hoverOverlay, label, rootRect);
        return root;
    }

    private static GameObject ApplyToMainMenu(
        Sprite panelSprite,
        GameObject secondaryPrefab,
        GameObject backPrefab
    )
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        GameObject canvas = GameObject.Find("MainMenuCanvas");

        if (canvas == null)
        {
            throw new MissingReferenceException("MainMenuCanvas was not found in Main Menu.");
        }

        Transform settingsPanel = canvas.transform.Find("SettingsPanel");
        Transform settingsWindow = settingsPanel?.Find("SettingsWindow");
        Transform footer = settingsWindow?.Find("Footer");

        if (settingsPanel == null || settingsWindow == null || footer == null)
        {
            throw new MissingReferenceException(
                "SettingsPanel/SettingsWindow/Footer hierarchy is incomplete."
            );
        }

        Image dimmer = settingsPanel.GetComponent<Image>();
        if (dimmer == null)
        {
            dimmer = settingsPanel.gameObject.AddComponent<Image>();
        }
        dimmer.sprite = null;
        dimmer.color = new Color(0f, 0f, 0f, 0.38f);
        dimmer.raycastTarget = true;

        RectTransform windowRect = (RectTransform)settingsWindow;
        windowRect.anchorMin = new Vector2(0.1f, 0.1f);
        windowRect.anchorMax = new Vector2(0.9f, 0.9f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = Vector2.zero;

        Image windowImage = settingsWindow.GetComponent<Image>();
        windowImage.sprite = panelSprite;
        windowImage.type = Image.Type.Sliced;
        windowImage.color = Color.white;
        windowImage.raycastTarget = true;

        Shadow shadow = settingsWindow.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = settingsWindow.gameObject.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
        shadow.effectDistance = new Vector2(0f, -8f);
        shadow.useGraphicAlpha = true;

        VerticalLayoutGroup windowLayout = settingsWindow.GetComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(72, 72, 42, 42);
        windowLayout.spacing = 18f;
        windowLayout.childAlignment = TextAnchor.UpperCenter;

        SetPreferredHeight(settingsWindow.Find("Header"), 82f);
        SetPreferredHeight(settingsWindow.Find("Footer"), 72f);

        HorizontalLayoutGroup footerLayout = footer.GetComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 24f;
        footerLayout.childAlignment = TextAnchor.MiddleCenter;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = false;
        footerLayout.childForceExpandHeight = false;

        DestroyChildIfPresent(footer, "DefaultButton");
        DestroyChildIfPresent(footer, "ApplyButton");
        DestroyChildIfPresent(footer, "BackButton");

        CreateFooterButton(secondaryPrefab, scene, footer, "DefaultButton", "DEFAULT", 0);
        CreateFooterButton(secondaryPrefab, scene, footer, "ApplyButton", "APPLY", 1);
        Button newBackButton = CreateFooterButton(
            backPrefab,
            scene,
            footer,
            "BackButton",
            "BACK",
            2
        );

        MainMenuUI menu = canvas.GetComponent<MainMenuUI>();
        SerializedObject serializedMenu = new SerializedObject(menu);
        serializedMenu.FindProperty("settingsBackButton").objectReferenceValue = newBackButton;
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(menu);

        GameObject windowPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
            settingsWindow.gameObject,
            SettingsWindowPrefabPath,
            InteractionMode.AutomatedAction
        );

        if (windowPrefab == null)
        {
            throw new UnityException($"Failed to create '{SettingsWindowPrefabPath}'.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new UnityException($"Failed to save '{MainMenuScenePath}'.");
        }

        return windowPrefab;
    }

    private static Button CreateFooterButton(
        GameObject prefab,
        Scene scene,
        Transform parent,
        string objectName,
        string labelText,
        int siblingIndex
    )
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = objectName;
        instance.transform.SetParent(parent, false);
        instance.transform.SetSiblingIndex(siblingIndex);
        instance.transform.Find("Label").GetComponent<TMP_Text>().text = labelText;
        return instance.GetComponent<Button>();
    }

    private static void DestroyChildIfPresent(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void SetPreferredHeight(Transform target, float height)
    {
        if (target == null)
        {
            return;
        }

        LayoutElement layout = target.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.preferredHeight = height;
        }
    }

    private static Image CreateImage(string name, Transform parent, int layer)
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        imageObject.layer = layer;
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private static TextMeshProUGUI CreateLabel(
        Transform parent,
        int layer,
        TMP_FontAsset font,
        string text
    )
    {
        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(Shadow)
        );
        labelObject.layer = layer;
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        Stretch(labelRect);
        labelRect.offsetMin = new Vector2(42f, 6f);
        labelRect.offsetMax = new Vector2(-24f, -6f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = font;
        label.fontSize = 22f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(213, 250, 255, 255);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        Shadow shadow = labelObject.GetComponent<Shadow>();
        shadow.effectColor = new Color32(0, 140, 255, 128);
        shadow.effectDistance = new Vector2(1f, -1f);
        return label;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
