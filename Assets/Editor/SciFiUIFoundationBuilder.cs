using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SciFiUIFoundationBuilder
{
    private const string ButtonSpritePath =
        "Assets/UIAssets/Assets Wenrexa UI Sci-Fi Minimalism #01/Main Game(Example)/Btn05.png";
    private const string FontAssetPath =
        "Assets/Fonts/orbito-font/OrbitoRegular-7ODqA SDF.asset";
    private const string PrefabFolder = "Assets/UI/Prefabs/Common";
    private const string PrefabPath = PrefabFolder + "/SciFiPrimaryButton.prefab";
    private const string HoverShaderPath = "Assets/Shaders/UIAlphaSilhouetteTint.shader";
    private const string MaterialFolder = "Assets/UI/Materials";
    private const string HoverMaterialPath = MaterialFolder + "/SciFiButtonHover.mat";

    [MenuItem("Tools/UI/Create Sci-Fi Primary Button Prefab")]
    public static void BuildFromMenu()
    {
        BuildPrefab(selectCreatedAsset: true);
    }

    private static void BuildPrefab(bool selectCreatedAsset)
    {
        Sprite buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Shader hoverShader = AssetDatabase.LoadAssetAtPath<Shader>(HoverShaderPath);

        if (buttonSprite == null || font == null || hoverShader == null)
        {
            Debug.LogError(
                "Cannot build SciFiPrimaryButton: Btn05 sprite, Orbito font, or hover shader is missing."
            );
            return;
        }

        EnsureFolder("Assets", "UI");
        EnsureFolder("Assets/UI", "Prefabs");
        EnsureFolder("Assets/UI/Prefabs", "Common");
        EnsureFolder("Assets/UI", "Materials");

        Material hoverMaterial = AssetDatabase.LoadAssetAtPath<Material>(HoverMaterialPath);
        if (hoverMaterial == null)
        {
            hoverMaterial = new Material(hoverShader);
            AssetDatabase.CreateAsset(hoverMaterial, HoverMaterialPath);
        }
        else if (hoverMaterial.shader != hoverShader)
        {
            hoverMaterial.shader = hoverShader;
            EditorUtility.SetDirty(hoverMaterial);
        }

        GameObject root = new GameObject(
            "SciFiPrimaryButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );

        try
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                root.layer = uiLayer;
            }

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(360f, 72f);

            Image background = root.GetComponent<Image>();
            background.sprite = buttonSprite;
            background.type = Image.Type.Sliced;
            background.preserveAspect = false;
            background.raycastTarget = true;
            // Keep the graphic neutral because Button ColorTint multiplies this color.
            // Tinting both the Image and the Button makes hover states too dark to read.
            background.color = Color.white;

            Button button = root.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(22, 63, 122, 255);
            colors.highlightedColor = new Color32(77, 244, 255, 255);
            colors.pressedColor = new Color32(255, 240, 166, 255);
            colors.selectedColor = new Color32(77, 244, 255, 255);
            colors.disabledColor = new Color32(38, 52, 79, 115);
            colors.colorMultiplier = 2f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Automatic;
            button.navigation = navigation;

            LayoutElement layoutElement = root.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 360f;
            layoutElement.preferredHeight = 72f;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            GameObject hoverObject = new GameObject(
                "HoverOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            hoverObject.transform.SetParent(root.transform, false);
            hoverObject.layer = root.layer;

            RectTransform hoverRect = hoverObject.GetComponent<RectTransform>();
            hoverRect.anchorMin = Vector2.zero;
            hoverRect.anchorMax = Vector2.one;
            hoverRect.offsetMin = Vector2.zero;
            hoverRect.offsetMax = Vector2.zero;

            Image hoverOverlay = hoverObject.GetComponent<Image>();
            hoverOverlay.sprite = buttonSprite;
            hoverOverlay.type = Image.Type.Sliced;
            hoverOverlay.preserveAspect = false;
            hoverOverlay.raycastTarget = false;
            hoverOverlay.material = hoverMaterial;
            hoverOverlay.color = Color.clear;

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI),
                typeof(Shadow)
            );
            labelObject.transform.SetParent(root.transform, false);
            labelObject.layer = root.layer;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = new Vector2(28f, 6f);
            labelRect.offsetMax = new Vector2(-28f, -6f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "NEW GAME";
            label.font = font;
            label.fontSize = 28f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(213, 250, 255, 255);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;

            Shadow shadow = labelObject.GetComponent<Shadow>();
            shadow.effectColor = new Color32(0, 140, 255, 128);
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;

            SciFiButtonHoverFeedback hoverFeedback = root.AddComponent<SciFiButtonHoverFeedback>();
            hoverFeedback.Configure(hoverOverlay, label, rootRect);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();

            if (prefab == null)
            {
                Debug.LogError($"Failed to create prefab at '{PrefabPath}'.");
                return;
            }

            if (selectCreatedAsset)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            Debug.Log($"Created Sci-Fi primary button prefab at '{PrefabPath}'.", prefab);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
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
