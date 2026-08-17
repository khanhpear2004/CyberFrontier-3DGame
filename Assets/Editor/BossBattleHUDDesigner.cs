using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BossBattleHUDDesigner : EditorWindow
{
    private BossHealthBarUI bossHealthBar;
    private ObjectiveHUD objectiveHUD;
    private float bossWidth = 760f;
    private float bossHeight = 201f;
    private float rightMargin = 48f;
    private float topMargin = 42f;
    private float objectiveGap = 16f;
    private float objectiveWidth = 360f;
    private float objectiveHeight = 72f;
    private bool hideLabels = true;
    private bool fillFromRight = true;
    private bool useMirroredPlayerFrame = true;
    private Color borderColor = new Color(0.72f, 0.92f, 0.14f, 1f);
    private Color backgroundColor = new Color(0.018f, 0.028f, 0.04f, 0.96f);
    private Color healthColor = new Color(0.76f, 1f, 0.12f, 1f);
    private Color damageColor = new Color(1f, 0.34f, 0.05f, 1f);

    [MenuItem("Tools/Gameplay UI/Boss HUD Designer")]
    private static void OpenWindow()
    {
        BossBattleHUDDesigner window = GetWindow<BossBattleHUDDesigner>("Boss HUD Designer");
        window.minSize = new Vector2(420f, 620f);
        window.ResolveSceneReferences();
    }

    private void OnEnable()
    {
        ResolveSceneReferences();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scene References", EditorStyles.boldLabel);
        bossHealthBar = (BossHealthBarUI)EditorGUILayout.ObjectField("Boss Health HUD", bossHealthBar, typeof(BossHealthBarUI), true);
        objectiveHUD = (ObjectiveHUD)EditorGUILayout.ObjectField("Objective HUD", objectiveHUD, typeof(ObjectiveHUD), true);

        if (GUILayout.Button("Find UI In Open Scene", GUILayout.Height(28f)))
        {
            ResolveSceneReferences();
        }

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Boss Bar Layout", EditorStyles.boldLabel);
        bossWidth = EditorGUILayout.Slider("Width", bossWidth, 360f, 900f);
        using (new EditorGUI.DisabledScope(useMirroredPlayerFrame))
        {
            bossHeight = EditorGUILayout.Slider(useMirroredPlayerFrame ? "Height (Auto From Player)" : "Height", bossHeight, 28f, 260f);
        }
        rightMargin = EditorGUILayout.Slider("Right Margin", rightMargin, 0f, 240f);
        topMargin = EditorGUILayout.Slider("Top Margin", topMargin, 0f, 240f);
        hideLabels = EditorGUILayout.Toggle("Hide Name And Value", hideLabels);
        fillFromRight = EditorGUILayout.Toggle("Drain Toward Right", fillFromRight);
        useMirroredPlayerFrame = EditorGUILayout.Toggle("Use Mirrored Orange Frame", useMirroredPlayerFrame);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
        borderColor = EditorGUILayout.ColorField("Border", borderColor);
        backgroundColor = EditorGUILayout.ColorField("Background", backgroundColor);
        healthColor = EditorGUILayout.ColorField("Health", healthColor);
        damageColor = EditorGUILayout.ColorField("Damage Trail", damageColor);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Objective Layout", EditorStyles.boldLabel);
        objectiveGap = EditorGUILayout.Slider("Gap Below Boss Bar", objectiveGap, 0f, 80f);
        objectiveWidth = EditorGUILayout.Slider("Width", objectiveWidth, 240f, 600f);
        objectiveHeight = EditorGUILayout.Slider("Height", objectiveHeight, 40f, 140f);

        EditorGUILayout.Space(12f);

        using (new EditorGUI.DisabledScope(bossHealthBar == null))
        {
            if (GUILayout.Button("Create / Upgrade ZZZ-Style Layout", GUILayout.Height(34f)))
            {
                ApplyLayout();
            }

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Preview 100%"))
            {
                PreviewHealth(1f);
            }

            if (GUILayout.Button("Preview 50%"))
            {
                PreviewHealth(0.5f);
            }

            if (GUILayout.Button("Preview 10%"))
            {
                PreviewHealth(0.1f);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Show HUD"))
            {
                SetPreviewVisibility(true);
            }

            if (GUILayout.Button("Hide HUD"))
            {
                SetPreviewVisibility(false);
            }

            if (GUILayout.Button("Select HUD"))
            {
                Selection.activeGameObject = bossHealthBar.gameObject;
                SceneView.FrameLastActiveSceneView();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(12f);
        DrawValidation();
        EditorGUILayout.HelpBox("Tool chỉ thay đổi UI khi bạn bấm nút. Mọi thao tác đều hỗ trợ Ctrl+Z. Sau khi hài lòng, lưu scene bằng Ctrl+S ở Edit Mode.", MessageType.Info);
    }

    private void ApplyLayout()
    {
        if (bossHealthBar == null)
        {
            return;
        }

        RectTransform root = bossHealthBar.transform as RectTransform;

        if (root == null)
        {
            Debug.LogError("BossHealthHUD must use RectTransform.", bossHealthBar);
            return;
        }

        Undo.RecordObject(root, "Apply Boss HUD Layout");
        root.anchorMin = Vector2.one;
        root.anchorMax = Vector2.one;
        root.pivot = Vector2.one;
        root.anchoredPosition = new Vector2(-rightMargin, -topMargin);
        root.sizeDelta = new Vector2(bossWidth, bossHeight);
        root.localScale = Vector3.one;

        Image legacyFrame = root.GetComponent<Image>();
        Sprite legacyFrameSprite = legacyFrame != null ? legacyFrame.sprite : null;

        if (legacyFrame != null)
        {
            Undo.RecordObject(legacyFrame, "Disable Stretched Boss Frame");
            legacyFrame.enabled = false;
        }

        if (useMirroredPlayerFrame && ApplyExactMirroredPlayerStyle(root))
        {
            SetLabelVisibility(FindText(root, "BossName"), false);
            SetLabelVisibility(FindText(root, "HealthValue"), false);
            ConfigureObjective(root);
            SetPreviewVisibility(true);
            MarkSceneDirty();
            Selection.activeGameObject = bossHealthBar.gameObject;
            SceneView.RepaintAll();
            return;
        }

        Image border = EnsureImage(root, "BarBorder");
        Image background = EnsureImage(root, "BarBackground");
        Image mirroredFrame = EnsureImage(root, "MirroredFrame");
        Image damageFill = FindImage(root, "DamageFill");
        Image healthFill = FindImage(root, "HealthFill");
        TMP_Text bossName = FindText(root, "BossName");
        TMP_Text healthValue = FindText(root, "HealthValue");

        ConfigureBarRegion(border, borderColor, useMirroredPlayerFrame, Vector2.zero, Vector2.zero);
        ConfigureBarRegion(background, backgroundColor, useMirroredPlayerFrame, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        ConfigureMirroredFrame(mirroredFrame, legacyFrameSprite, useMirroredPlayerFrame);
        border.transform.SetSiblingIndex(0);
        background.transform.SetSiblingIndex(1);
        mirroredFrame.transform.SetAsLastSibling();

        if (damageFill != null)
        {
            ConfigureFill(damageFill, damageColor, fillFromRight, background.rectTransform);
        }

        if (healthFill != null)
        {
            ConfigureFill(healthFill, healthColor, fillFromRight, background.rectTransform);
        }

        SetLabelVisibility(bossName, !hideLabels);
        SetLabelVisibility(healthValue, !hideLabels);
        ConfigureObjective(root);
        AssignBossHealthReferences(healthFill, damageFill, healthValue);
        SetPreviewVisibility(true);
        MarkSceneDirty();
        Selection.activeGameObject = bossHealthBar.gameObject;
        SceneView.RepaintAll();
    }

    private void ConfigureObjective(RectTransform bossRoot)
    {
        if (objectiveHUD == null || objectiveHUD.transform is not RectTransform objectiveRect)
        {
            return;
        }

        Undo.RecordObject(objectiveRect, "Align Objective HUD");
        objectiveRect.anchorMin = Vector2.one;
        objectiveRect.anchorMax = Vector2.one;
        objectiveRect.pivot = Vector2.one;
        objectiveRect.anchoredPosition = new Vector2(-rightMargin, -(topMargin + bossRoot.sizeDelta.y + objectiveGap));
        objectiveRect.sizeDelta = new Vector2(objectiveWidth, objectiveHeight);
        objectiveRect.localScale = Vector3.one;
        UpgradeObjectiveVisual(objectiveRect);
    }

    private bool ApplyExactMirroredPlayerStyle(RectTransform bossRoot)
    {
        RectTransform playerHealthFrame = FindPlayerHealthFrame(bossRoot);

        if (playerHealthFrame == null)
        {
            Debug.LogError("Không tìm thấy UI/Canvas/StatusBar/.../HealthFrame của player để clone.", bossHealthBar);
            return false;
        }

        Transform existingClone = bossRoot.Find("MirroredPlayerHealthFrame");

        if (existingClone != null)
        {
            Undo.DestroyObjectImmediate(existingClone.gameObject);
        }

        GameObject clonedFrameObject = Instantiate(playerHealthFrame.gameObject);
        clonedFrameObject.name = "MirroredPlayerHealthFrame";
        Undo.RegisterCreatedObjectUndo(clonedFrameObject, "Clone Player Health Frame For Boss");
        Undo.SetTransformParent(clonedFrameObject.transform, bossRoot, "Parent Mirrored Boss Health Frame");

        RectTransform clonedFrame = clonedFrameObject.GetComponent<RectTransform>();
        float sourceWidth = Mathf.Max(1f, playerHealthFrame.rect.width);
        float sourceHeight = Mathf.Max(1f, playerHealthFrame.rect.height);
        float uniformScale = bossWidth / sourceWidth;
        float exactHeight = sourceHeight * uniformScale;
        Undo.RecordObject(bossRoot, "Lock Boss HUD Player Aspect Ratio");
        bossRoot.sizeDelta = new Vector2(bossWidth, exactHeight);
        bossHeight = exactHeight;
        Undo.RecordObject(clonedFrame, "Mirror Player Health Frame");
        clonedFrame.anchorMin = new Vector2(0.5f, 0.5f);
        clonedFrame.anchorMax = new Vector2(0.5f, 0.5f);
        clonedFrame.pivot = new Vector2(0.5f, 0.5f);
        clonedFrame.anchoredPosition = Vector2.zero;
        clonedFrame.sizeDelta = playerHealthFrame.sizeDelta;
        clonedFrame.localScale = new Vector3(-uniformScale, uniformScale, 1f);

        Image frameBackground = FindImage(clonedFrame, "Background");
        Image damageFill = FindImage(clonedFrame, "DelayFill");
        Image healthFill = FindImage(clonedFrame, "Filled");
        Sprite yellowFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SciFi-UI/Game UI collection FREE version/PNG/Bars/Yellow/x1/Asset 6.png");
        Sprite yellowFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SciFi-UI/Game UI collection FREE version/PNG/Bars/Yellow/x1/Asset 2.png");
        Sprite whiteFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SciFi-UI/Game UI collection FREE version/PNG/Bars/white/x1/Asset 2.png");

        if (frameBackground == null || damageFill == null || healthFill == null || yellowFrameSprite == null || yellowFillSprite == null || whiteFillSprite == null)
        {
            Debug.LogError("Thiếu Background, DelayFill, Filled hoặc sprite Yellow/White trong SciFi-UI asset pack.", clonedFrameObject);
            return false;
        }

        Undo.RecordObjects(new Object[] { frameBackground, damageFill, healthFill }, "Apply Yellow Boss Health Style");
        frameBackground.sprite = yellowFrameSprite;
        frameBackground.color = Color.white;
        damageFill.sprite = whiteFillSprite;
        damageFill.color = damageColor;
        damageFill.type = Image.Type.Filled;
        damageFill.fillMethod = Image.FillMethod.Horizontal;
        damageFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFill.sprite = yellowFillSprite;
        healthFill.color = Color.white;
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;

        SetChildActive(bossRoot, "BarBorder", false);
        SetChildActive(bossRoot, "BarBackground", false);
        SetChildActive(bossRoot, "MirroredFrame", false);
        clonedFrame.SetAsLastSibling();
        AssignBossHealthReferences(healthFill, damageFill, null);
        return true;
    }

    private static RectTransform FindPlayerHealthFrame(RectTransform bossRoot)
    {
        foreach (RectTransform candidate in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (candidate.name == "HealthFrame" && candidate.gameObject.scene.IsValid() && candidate.gameObject.scene.isLoaded && !candidate.IsChildOf(bossRoot))
            {
                return candidate;
            }
        }

        return null;
    }

    private static void SetChildActive(RectTransform root, string childName, bool shouldShow)
    {
        Transform child = root.Find(childName);

        if (child == null)
        {
            return;
        }

        Undo.RecordObject(child.gameObject, "Toggle Legacy Boss HUD Visual");
        child.gameObject.SetActive(shouldShow);
    }

    private static void UpgradeObjectiveVisual(RectTransform objectiveRect)
    {
        Sprite objectiveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SciFi-UI/Game UI collection FREE version/PNG/Bars/Blue/x1/Asset 11.png");

        if (objectiveSprite == null)
        {
            Debug.LogError("Không load được Objective sprite Bars/Blue/x1/Asset 11. Tool giữ nguyên visual hiện tại.", objectiveRect);
            return;
        }

        Image objectiveFrame = objectiveRect.GetComponent<Image>();
        bool frameUsesObjectiveRoot = objectiveFrame != null;

        if (objectiveFrame == null)
        {
            objectiveFrame = EnsureImage(objectiveRect, "ObjectiveFrame");
            objectiveFrame.transform.SetSiblingIndex(0);
        }

        Undo.RecordObjects(new Object[] { objectiveFrame, objectiveFrame.rectTransform }, "Upgrade Objective Frame");
        objectiveFrame.sprite = objectiveSprite;
        objectiveFrame.type = Image.Type.Simple;
        objectiveFrame.preserveAspect = false;
        objectiveFrame.color = Color.white;
        objectiveFrame.raycastTarget = false;

        if (!frameUsesObjectiveRoot)
        {
            objectiveFrame.rectTransform.anchorMin = Vector2.zero;
            objectiveFrame.rectTransform.anchorMax = Vector2.one;
            objectiveFrame.rectTransform.offsetMin = Vector2.zero;
            objectiveFrame.rectTransform.offsetMax = Vector2.zero;
        }

        TMP_Text objectiveText = objectiveRect.GetComponentInChildren<TMP_Text>(true);

        if (objectiveText == null)
        {
            return;
        }

        Undo.RecordObjects(new Object[] { objectiveText, objectiveText.rectTransform }, "Upgrade Objective Text");
        objectiveText.fontSize = 22f;
        objectiveText.fontStyle = FontStyles.Bold;
        objectiveText.alignment = TextAlignmentOptions.MidlineLeft;
        objectiveText.color = Color.white;
        objectiveText.textWrappingMode = TextWrappingModes.NoWrap;
        objectiveText.rectTransform.anchorMin = Vector2.zero;
        objectiveText.rectTransform.anchorMax = Vector2.one;
        objectiveText.rectTransform.offsetMin = new Vector2(72f, 8f);
        objectiveText.rectTransform.offsetMax = new Vector2(-18f, -8f);
    }

    private static void ConfigureStretchImage(Image image, Color color, Vector2 offsetMin, Vector2 offsetMax)
    {
        Undo.RecordObjects(new Object[] { image, image.rectTransform }, "Configure HUD Image");
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = offsetMin;
        image.rectTransform.offsetMax = offsetMax;
        image.rectTransform.localScale = Vector3.one;
    }

    private static void ConfigureBarRegion(Image image, Color color, bool reserveMirroredCap, Vector2 offsetMin, Vector2 offsetMax)
    {
        Undo.RecordObjects(new Object[] { image, image.rectTransform }, "Configure Boss Bar Region");
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = reserveMirroredCap ? new Vector2(0.055f, 0.22f) : Vector2.zero;
        image.rectTransform.anchorMax = reserveMirroredCap ? new Vector2(0.76f, 0.56f) : Vector2.one;
        image.rectTransform.offsetMin = offsetMin;
        image.rectTransform.offsetMax = offsetMax;
        image.rectTransform.localScale = Vector3.one;
    }

    private static void ConfigureMirroredFrame(Image frame, Sprite frameSprite, bool shouldShow)
    {
        Undo.RecordObjects(new Object[] { frame, frame.rectTransform, frame.gameObject }, "Configure Mirrored Boss Frame");
        frame.gameObject.SetActive(shouldShow);
        frame.sprite = frameSprite;
        frame.type = Image.Type.Simple;
        frame.preserveAspect = false;
        frame.color = Color.white;
        frame.raycastTarget = false;
        frame.rectTransform.anchorMin = Vector2.zero;
        frame.rectTransform.anchorMax = Vector2.one;
        frame.rectTransform.offsetMin = Vector2.zero;
        frame.rectTransform.offsetMax = Vector2.zero;
        frame.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        if (shouldShow && frameSprite == null)
        {
            Debug.LogWarning("BossHealthHUD root không còn Source Image để mirror. Hãy gắn lại sprite khung cam vào Image của BossHealthHUD rồi Apply lần nữa.", frame);
        }
    }

    private static void ConfigureFill(Image image, Color color, bool fillFromRight, RectTransform newParent)
    {
        Undo.SetTransformParent(image.transform, newParent, "Move Boss Health Fill");
        Undo.RecordObjects(new Object[] { image, image.rectTransform }, "Configure Boss Health Fill");
        image.sprite = null;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = fillFromRight ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = new Vector2(5f, 5f);
        image.rectTransform.offsetMax = new Vector2(-5f, -5f);
        image.rectTransform.localScale = Vector3.one;
    }

    private static Image EnsureImage(RectTransform parent, string objectName)
    {
        Image existing = FindImage(parent, objectName);

        if (existing != null)
        {
            return existing;
        }

        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(imageObject, $"Create {objectName}");
        Undo.SetTransformParent(imageObject.transform, parent, $"Parent {objectName}");
        imageObject.layer = parent.gameObject.layer;
        return imageObject.GetComponent<Image>();
    }

    private static Image FindImage(RectTransform root, string objectName)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name == objectName)
            {
                return image;
            }
        }

        return null;
    }

    private static TMP_Text FindText(RectTransform root, string objectName)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == objectName)
            {
                return text;
            }
        }

        return null;
    }

    private static void SetLabelVisibility(TMP_Text label, bool shouldShow)
    {
        if (label == null)
        {
            return;
        }

        Undo.RecordObject(label.gameObject, "Toggle Boss HUD Label");
        label.gameObject.SetActive(shouldShow);
    }

    private void AssignBossHealthReferences(Image healthFill, Image damageFill, TMP_Text healthValue)
    {
        SerializedObject serializedHUD = new SerializedObject(bossHealthBar);
        serializedHUD.FindProperty("canvasGroup").objectReferenceValue = bossHealthBar.GetComponent<CanvasGroup>();
        serializedHUD.FindProperty("healthFill").objectReferenceValue = healthFill;
        serializedHUD.FindProperty("delayedFill").objectReferenceValue = damageFill;
        serializedHUD.FindProperty("healthText").objectReferenceValue = hideLabels ? null : healthValue;
        serializedHUD.ApplyModifiedProperties();
    }

    private void PreviewHealth(float normalizedHealth)
    {
        if (bossHealthBar == null)
        {
            return;
        }

        RectTransform root = bossHealthBar.transform as RectTransform;
        Image healthFill = FindImage(root, "HealthFill");
        Image damageFill = FindImage(root, "DamageFill");

        if (healthFill != null)
        {
            Undo.RecordObject(healthFill, "Preview Boss Health");
            healthFill.fillAmount = normalizedHealth;
        }

        if (damageFill != null)
        {
            Undo.RecordObject(damageFill, "Preview Boss Damage Trail");
            damageFill.fillAmount = Mathf.Min(1f, normalizedHealth + 0.18f);
        }

        SetPreviewVisibility(true);
        SceneView.RepaintAll();
    }

    private void SetPreviewVisibility(bool shouldShow)
    {
        if (bossHealthBar == null)
        {
            return;
        }

        CanvasGroup canvasGroup = bossHealthBar.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            return;
        }

        Undo.RecordObject(canvasGroup, "Preview Boss HUD Visibility");
        canvasGroup.alpha = shouldShow ? 1f : 0f;
        SceneView.RepaintAll();
    }

    private void ResolveSceneReferences()
    {
        bossHealthBar = FindSceneObject<BossHealthBarUI>();
        objectiveHUD = FindSceneObject<ObjectiveHUD>();
        Repaint();
    }

    private static T FindSceneObject<T>() where T : Component
    {
        foreach (T candidate in Resources.FindObjectsOfTypeAll<T>())
        {
            if (candidate.gameObject.scene.IsValid() && candidate.gameObject.scene.isLoaded)
            {
                return candidate;
            }
        }

        return null;
    }

    private void DrawValidation()
    {
        if (bossHealthBar == null)
        {
            EditorGUILayout.HelpBox("Không tìm thấy BossHealthBarUI trong scene đang mở.", MessageType.Error);
            return;
        }

        RectTransform root = bossHealthBar.transform as RectTransform;
        bool hasHealthFill = root != null && FindImage(root, "HealthFill") != null;
        bool hasDamageFill = root != null && FindImage(root, "DamageFill") != null;

        if (!hasHealthFill || !hasDamageFill)
        {
            EditorGUILayout.HelpBox("Thiếu HealthFill hoặc DamageFill. Tool cần hai object này trước khi preview.", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox("Boss HUD references hợp lệ. Bạn có thể Apply Layout hoặc dùng các nút Preview.", MessageType.Info);
    }

    private static void MarkSceneDirty()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }
}
