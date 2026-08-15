using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public sealed class MainMenuVisualPolish : MonoBehaviour
{
    private static readonly Color Cyan = new(0.08f, 0.9f, 0.96f, 1f);
    private static readonly Color PrimaryText = new(0.88f, 0.98f, 1f, 1f);
    private static readonly Color SecondaryText = new(0.48f, 0.7f, 0.76f, 1f);
    private static readonly Color PanelColor = new(0.018f, 0.035f, 0.065f, 0.88f);

    [SerializeField] private Sprite panelSprite;

    private void Awake()
    {
        ApplyVisualPolish();
    }

    public void BakeIntoScene()
    {
        ApplyVisualPolish();
    }

    private void ApplyVisualPolish()
    {
        RectTransform root = transform as RectTransform;
        RectTransform titleRect = transform.Find("GameTitle") as RectTransform;
        RectTransform buttonContainer = transform.Find("MenuButtonContainer") as RectTransform;

        if (root == null || titleRect == null || buttonContainer == null)
        {
            Debug.LogError("MainMenuVisualPolish requires GameTitle and MenuButtonContainer.", this);
            return;
        }

        TextMeshProUGUI title = titleRect.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = title != null ? title.font : null;

        CreateAtmosphere(root);
        CreateMenuPanel(root, font);
        StyleTitle(titleRect, title, font);
        StyleButtons(buttonContainer);
        CreateFooter(root, font);

        titleRect.SetAsLastSibling();
        buttonContainer.SetAsLastSibling();
    }

    private static void CreateAtmosphere(RectTransform root)
    {
        RectTransform leftShade = CreateRect("LeftFocusGradient", root);
        Stretch(leftShade);
        MainMenuGradientGraphic leftGradient = leftShade.GetComponent<MainMenuGradientGraphic>() ??
                                               leftShade.gameObject.AddComponent<MainMenuGradientGraphic>();
        leftGradient.Configure(
            new Color(0.005f, 0.012f, 0.026f, 0.82f),
            new Color(0.005f, 0.012f, 0.026f, 0f),
            false);
        leftShade.SetSiblingIndex(0);

        RectTransform bottomShade = CreateRect("BottomVignette", root);
        bottomShade.anchorMin = new Vector2(0f, 0f);
        bottomShade.anchorMax = new Vector2(1f, 0.32f);
        bottomShade.offsetMin = Vector2.zero;
        bottomShade.offsetMax = Vector2.zero;
        MainMenuGradientGraphic bottomGradient = bottomShade.GetComponent<MainMenuGradientGraphic>() ??
                                                 bottomShade.gameObject.AddComponent<MainMenuGradientGraphic>();
        bottomGradient.Configure(
            new Color(0.005f, 0.012f, 0.022f, 0.78f),
            new Color(0.005f, 0.012f, 0.022f, 0f),
            true);
        bottomShade.SetSiblingIndex(1);

        RectTransform topRule = CreateImage(
            "TopAccentRule",
            root,
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.38f)).rectTransform;
        topRule.anchorMin = new Vector2(0f, 1f);
        topRule.anchorMax = new Vector2(1f, 1f);
        topRule.pivot = new Vector2(0.5f, 1f);
        topRule.anchoredPosition = new Vector2(0f, -26f);
        topRule.sizeDelta = new Vector2(-144f, 2f);
        topRule.SetSiblingIndex(2);
    }

    private void CreateMenuPanel(RectTransform root, TMP_FontAsset font)
    {
        Image panel = CreateImage("NavigationPanel", root, PanelColor);
        panel.sprite = panelSprite;
        panel.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        panel.raycastTarget = false;

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(82f, -18f);
        panelRect.sizeDelta = new Vector2(530f, 570f);
        panelRect.SetSiblingIndex(3);

        RectTransform rail = CreateImage("AccentRail", root, Cyan).rectTransform;
        rail.anchorMin = new Vector2(0f, 0.5f);
        rail.anchorMax = new Vector2(0f, 0.5f);
        rail.pivot = new Vector2(0f, 0.5f);
        rail.anchoredPosition = new Vector2(82f, -18f);
        rail.sizeDelta = new Vector2(4f, 470f);

        TextMeshProUGUI kicker = CreateText(
            "MenuKicker",
            root,
            font,
            "COMMAND  //  01",
            20f,
            SecondaryText,
            TextAlignmentOptions.MidlineLeft,
            FontStyles.Bold);
        SetAnchoredRect(kicker.rectTransform, new Vector2(0f, 0.5f), new Vector2(128f, 192f),
            new Vector2(390f, 34f), new Vector2(0f, 0.5f));
        kicker.characterSpacing = 4f;

        TextMeshProUGUI sectionTitle = CreateText(
            "MenuSectionTitle",
            root,
            font,
            "MAIN MENU",
            38f,
            PrimaryText,
            TextAlignmentOptions.MidlineLeft,
            FontStyles.Bold);
        SetAnchoredRect(sectionTitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(128f, 145f),
            new Vector2(400f, 54f), new Vector2(0f, 0.5f));
        sectionTitle.characterSpacing = 2f;

        RectTransform separator = CreateImage(
            "MenuSeparator",
            root,
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.68f)).rectTransform;
        SetAnchoredRect(separator, new Vector2(0f, 0.5f), new Vector2(128f, 111f),
            new Vector2(380f, 2f), new Vector2(0f, 0.5f));

        TextMeshProUGUI hint = CreateText(
            "MenuHint",
            root,
            font,
            "SELECT AN OPERATION",
            17f,
            SecondaryText,
            TextAlignmentOptions.MidlineLeft,
            FontStyles.Normal);
        SetAnchoredRect(hint.rectTransform, new Vector2(0f, 0.5f), new Vector2(128f, -217f),
            new Vector2(390f, 30f), new Vector2(0f, 0.5f));
        hint.characterSpacing = 3f;
    }

    private static void StyleTitle(
        RectTransform titleRect,
        TextMeshProUGUI title,
        TMP_FontAsset font)
    {
        titleRect.anchorMin = new Vector2(1f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(1f, 1f);
        titleRect.anchoredPosition = new Vector2(-92f, -72f);
        titleRect.sizeDelta = new Vector2(850f, 112f);

        if (title != null)
        {
            title.fontSize = 82f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.MidlineRight;
            title.color = Cyan;
            title.characterSpacing = 1.5f;

            Shadow shadow = title.GetComponent<Shadow>() ?? title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.08f, 0.12f, 0.9f);
            shadow.effectDistance = new Vector2(3f, -3f);
        }

        TextMeshProUGUI subtitle = CreateText(
            "GameSubtitle",
            titleRect.parent,
            font,
            "SURVIVE THE RED WORLD",
            24f,
            PrimaryText,
            TextAlignmentOptions.MidlineRight,
            FontStyles.Normal);
        SetAnchoredRect(subtitle.rectTransform, new Vector2(1f, 1f), new Vector2(-96f, -170f),
            new Vector2(700f, 42f), new Vector2(1f, 1f));
        subtitle.characterSpacing = 5f;

        TextMeshProUGUI designation = CreateText(
            "GameDesignation",
            titleRect.parent,
            font,
            "FRONTIER DIVISION  /  SECTOR 07",
            16f,
            SecondaryText,
            TextAlignmentOptions.MidlineRight,
            FontStyles.Normal);
        SetAnchoredRect(designation.rectTransform, new Vector2(1f, 1f), new Vector2(-96f, -209f),
            new Vector2(700f, 30f), new Vector2(1f, 1f));
        designation.characterSpacing = 3f;
    }

    private static void StyleButtons(RectTransform buttonContainer)
    {
        buttonContainer.anchorMin = new Vector2(0f, 0.5f);
        buttonContainer.anchorMax = new Vector2(0f, 0.5f);
        buttonContainer.pivot = new Vector2(0f, 0.5f);
        buttonContainer.anchoredPosition = new Vector2(126f, -52f);
        buttonContainer.sizeDelta = new Vector2(410f, 270f);

        if (buttonContainer.TryGetComponent(out VerticalLayoutGroup layout))
        {
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
        }

        foreach (RectTransform buttonRect in buttonContainer)
        {
            buttonRect.sizeDelta = new Vector2(400f, 76f);

            if (buttonRect.TryGetComponent(out LayoutElement layoutElement))
            {
                layoutElement.preferredWidth = 400f;
                layoutElement.preferredHeight = 76f;
            }

            TextMeshProUGUI label = buttonRect.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.fontSize = 27f;
                label.characterSpacing = 1.5f;
            }
        }
    }

    private static void CreateFooter(RectTransform root, TMP_FontAsset font)
    {
        RectTransform footerRule = CreateImage(
            "FooterRule",
            root,
            new Color(Cyan.r, Cyan.g, Cyan.b, 0.32f)).rectTransform;
        footerRule.anchorMin = new Vector2(0f, 0f);
        footerRule.anchorMax = new Vector2(1f, 0f);
        footerRule.pivot = new Vector2(0.5f, 0f);
        footerRule.anchoredPosition = new Vector2(0f, 64f);
        footerRule.sizeDelta = new Vector2(-144f, 2f);

        TextMeshProUGUI version = CreateText(
            "VersionText",
            root,
            font,
            $"BUILD {Application.version}  //  PRE-ALPHA",
            16f,
            SecondaryText,
            TextAlignmentOptions.MidlineLeft,
            FontStyles.Normal);
        SetAnchoredRect(version.rectTransform, new Vector2(0f, 0f), new Vector2(76f, 24f),
            new Vector2(540f, 32f), new Vector2(0f, 0f));
        version.characterSpacing = 2f;

        TextMeshProUGUI status = CreateText(
            "SystemStatusText",
            root,
            font,
            "SYSTEM READY  //  LOCAL",
            16f,
            Cyan,
            TextAlignmentOptions.MidlineRight,
            FontStyles.Bold);
        SetAnchoredRect(status.rectTransform, new Vector2(1f, 0f), new Vector2(-76f, 24f),
            new Vector2(500f, 32f), new Vector2(1f, 0f));
        status.characterSpacing = 2f;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing is RectTransform existingRect)
        {
            return existingRect;
        }

        GameObject child = new(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Image image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        TMP_FontAsset font,
        string content,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        FontStyles style)
    {
        RectTransform rect = CreateRect(objectName, parent);
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>() ??
                               rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchoredRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        Vector2 pivot)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
