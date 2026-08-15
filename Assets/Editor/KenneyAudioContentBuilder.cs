using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class KenneyAudioContentBuilder
{
    private const string InterfaceFolder =
        "Assets/Audio/kenney_interface-sounds/Audio";
    private const string SciFiFolder =
        "Assets/Audio/kenney_sci-fi-sounds/Audio";
    private const string CueRoot = "Assets/Audio/Data/Cues";
    private const string CyberScenePath = "Assets/Scenes/CyberScene.unity";

    private static readonly Dictionary<string, AudioCue> Cues = new();

    [MenuItem("Tools/Audio/Build Kenney Audio Content")]
    public static void BuildFromMenu()
    {
        Build(selectCreatedAsset: true);
    }

    public static void Build(bool selectCreatedAsset)
    {
        EnsureFolder($"{CueRoot}/UI");
        EnsureFolder($"{CueRoot}/Weapons");
        EnsureFolder($"{CueRoot}/Impacts");
        EnsureFolder($"{CueRoot}/Interaction");
        EnsureFolder($"{CueRoot}/Environment");

        NormalizeKenneyImportSettings();
        CreateCues();
        ConnectButtonPrefabs();
        ConnectGameplayPrefabs();
        ConnectCyberScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (selectCreatedAsset && Cues.TryGetValue("UI_ButtonClick", out AudioCue cue))
        {
            Selection.activeObject = cue;
            EditorGUIUtility.PingObject(cue);
        }

        Debug.Log(
            "Kenney audio content built: import settings, AudioCues, UI buttons and gameplay references."
        );
    }

    private static void NormalizeKenneyImportSettings()
    {
        ConfigureFolder(InterfaceFolder, AudioCompressionFormat.PCM);
        ConfigureFolder(SciFiFolder, AudioCompressionFormat.ADPCM);
    }

    private static void ConfigureFolder(string folder, AudioCompressionFormat format)
    {
        string[] paths = AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToArray();

        foreach (string path in paths)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                continue;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool alreadyConfigured =
                settings.loadType == AudioClipLoadType.DecompressOnLoad &&
                settings.compressionFormat == format &&
                settings.sampleRateSetting == AudioSampleRateSetting.PreserveSampleRate &&
                settings.preloadAudioData &&
                !importer.loadInBackground &&
                importer.forceToMono;

            if (alreadyConfigured)
            {
                continue;
            }

            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = format;
            settings.quality = 1f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }
    }

    private static void CreateCues()
    {
        CreateCue(
            "UI_ButtonHover",
            $"{CueRoot}/UI_ButtonHover.asset",
            AudioBus.UI,
            Interface("select_001", "select_002", "select_003"),
            new Vector2(0.25f, 0.4f),
            new Vector2(0.98f, 1.02f),
            false,
            0f,
            1f,
            10f,
            32
        );
        CreateCue(
            "UI_ButtonClick",
            $"{CueRoot}/UI_ButtonClick.asset",
            AudioBus.UI,
            Interface("click_001", "click_003", "click_004"),
            new Vector2(0.5f, 0.7f),
            new Vector2(0.98f, 1.02f),
            false,
            0f,
            1f,
            10f,
            24
        );
        CreateCue(
            "UI_ButtonBack",
            $"{CueRoot}/UI/UI_ButtonBack.asset",
            AudioBus.UI,
            Interface("back_001", "back_002", "back_003", "back_004"),
            new Vector2(0.45f, 0.65f),
            new Vector2(0.98f, 1.02f),
            false,
            0f,
            1f,
            10f,
            24
        );
        CreateCue(
            "UI_Confirm",
            $"{CueRoot}/UI/UI_Confirm.asset",
            AudioBus.UI,
            Interface("confirmation_001", "confirmation_002", "confirmation_003"),
            new Vector2(0.5f, 0.7f),
            Vector2.one,
            false,
            0f,
            1f,
            10f,
            20
        );
        CreateCue(
            "UI_Error",
            $"{CueRoot}/UI/UI_Error.asset",
            AudioBus.UI,
            Interface("error_001", "error_002", "error_003"),
            new Vector2(0.5f, 0.7f),
            Vector2.one,
            false,
            0f,
            1f,
            10f,
            16
        );

        CreateCue(
            "SFX_PlayerLaserFire",
            $"{CueRoot}/SFX_PlayerLaserFire.asset",
            AudioBus.Sfx,
            SciFi("laserSmall_000", "laserSmall_001", "laserSmall_002", "laserSmall_003", "laserSmall_004"),
            new Vector2(0.65f, 0.8f),
            new Vector2(0.96f, 1.04f),
            false,
            0.2f,
            1f,
            35f,
            48
        );
        CreateCue(
            "SFX_EnemyLaserFire",
            $"{CueRoot}/Weapons/SFX_EnemyLaserFire.asset",
            AudioBus.Sfx,
            SciFi("laserLarge_000", "laserLarge_001", "laserLarge_002", "laserRetro_000", "laserRetro_001"),
            new Vector2(0.6f, 0.8f),
            new Vector2(0.94f, 1.06f),
            false,
            1f,
            2f,
            45f,
            80
        );
        CreateCue(
            "SFX_ImpactMetal",
            $"{CueRoot}/Impacts/SFX_ImpactMetal.asset",
            AudioBus.Sfx,
            SciFi("impactMetal_000", "impactMetal_001", "impactMetal_002", "impactMetal_003", "impactMetal_004"),
            new Vector2(0.55f, 0.75f),
            new Vector2(0.9f, 1.1f),
            false,
            1f,
            1f,
            30f,
            112
        );
        CreateCue(
            "SFX_ExplosionCrunch",
            $"{CueRoot}/Impacts/SFX_ExplosionCrunch.asset",
            AudioBus.Sfx,
            SciFi("explosionCrunch_000", "explosionCrunch_001", "explosionCrunch_002", "explosionCrunch_003", "explosionCrunch_004"),
            new Vector2(0.75f, 1f),
            new Vector2(0.94f, 1.04f),
            false,
            1f,
            3f,
            60f,
            64
        );
        CreateCue(
            "SFX_ShieldDeflect",
            $"{CueRoot}/Impacts/SFX_ShieldDeflect.asset",
            AudioBus.Sfx,
            SciFi("forceField_000", "forceField_001", "forceField_002"),
            new Vector2(0.55f, 0.75f),
            new Vector2(0.96f, 1.04f),
            false,
            1f,
            2f,
            35f,
            72
        );
        CreateCue(
            "SFX_ChestOpen",
            $"{CueRoot}/Interaction/SFX_ChestOpen.asset",
            AudioBus.Sfx,
            SciFi("doorOpen_000", "doorOpen_001", "doorOpen_002"),
            new Vector2(0.6f, 0.8f),
            new Vector2(0.97f, 1.03f),
            false,
            1f,
            2f,
            25f,
            96
        );
        CreateCue(
            "AMB_ComputerLoop",
            $"{CueRoot}/Environment/AMB_ComputerLoop.asset",
            AudioBus.Ambience,
            SciFi("computerNoise_000", "computerNoise_001", "computerNoise_002", "computerNoise_003"),
            new Vector2(0.22f, 0.35f),
            new Vector2(0.98f, 1.02f),
            true,
            1f,
            2f,
            20f,
            180
        );
        CreateCue(
            "AMB_EngineLoop",
            $"{CueRoot}/Environment/AMB_EngineLoop.asset",
            AudioBus.Ambience,
            SciFi("engineCircular_000", "engineCircular_001", "engineCircular_002"),
            new Vector2(0.25f, 0.4f),
            new Vector2(0.98f, 1.02f),
            true,
            1f,
            3f,
            30f,
            176
        );
    }

    private static void CreateCue(
        string key,
        string assetPath,
        AudioBus bus,
        IReadOnlyList<string> clipPaths,
        Vector2 volumeRange,
        Vector2 pitchRange,
        bool loop,
        float spatialBlend,
        float minDistance,
        float maxDistance,
        int priority
    )
    {
        AudioCue cue = AssetDatabase.LoadAssetAtPath<AudioCue>(assetPath);
        if (cue == null)
        {
            cue = ScriptableObject.CreateInstance<AudioCue>();
            AssetDatabase.CreateAsset(cue, assetPath);
        }

        SerializedObject serializedCue = new SerializedObject(cue);
        SerializedProperty clips = serializedCue.FindProperty("clips");
        clips.arraySize = clipPaths.Count;

        for (int index = 0; index < clipPaths.Count; index++)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPaths[index]);
            if (clip == null)
            {
                throw new MissingReferenceException($"AudioClip missing: '{clipPaths[index]}'.");
            }

            clips.GetArrayElementAtIndex(index).objectReferenceValue = clip;
        }

        serializedCue.FindProperty("bus").enumValueIndex = (int)bus;
        serializedCue.FindProperty("outputOverride").objectReferenceValue = null;
        serializedCue.FindProperty("volumeRange").vector2Value = volumeRange;
        serializedCue.FindProperty("pitchRange").vector2Value = pitchRange;
        serializedCue.FindProperty("loop").boolValue = loop;
        serializedCue.FindProperty("spatialBlend").floatValue = spatialBlend;
        serializedCue.FindProperty("minDistance").floatValue = minDistance;
        serializedCue.FindProperty("maxDistance").floatValue = maxDistance;
        serializedCue.FindProperty("priority").intValue = priority;
        serializedCue.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cue);
        Cues[key] = cue;
    }

    private static void ConnectButtonPrefabs()
    {
        ConfigurePrefab<UIAudioFeedback>(
            "Assets/UI/Prefabs/Common/SciFiPrimaryButton.prefab",
            component =>
            {
                SetReference(component, "hoverCue", Cues["UI_ButtonHover"]);
                SetReference(component, "clickCue", Cues["UI_ButtonClick"]);
            }
        );
        ConfigurePrefab<UIAudioFeedback>(
            "Assets/UI/Prefabs/Common/SciFiSecondaryButton.prefab",
            component =>
            {
                SetReference(component, "hoverCue", Cues["UI_ButtonHover"]);
                SetReference(component, "clickCue", Cues["UI_ButtonClick"]);
            }
        );
        ConfigurePrefab<UIAudioFeedback>(
            "Assets/UI/Prefabs/Common/SciFiBackButton.prefab",
            component =>
            {
                SetReference(component, "hoverCue", Cues["UI_ButtonHover"]);
                SetReference(component, "clickCue", Cues["UI_ButtonBack"]);
            }
        );
    }

    private static void ConnectGameplayPrefabs()
    {
        ConfigurePrefab<EnemyShooter>(
            "Assets/Prefab/Enemy/EnemyRobot.prefab",
            component => SetReference(component, "fireCue", Cues["SFX_EnemyLaserFire"])
        );
        ConfigurePrefab<EnemyShooter>(
            "Assets/Prefab/Enemy/Melee/EnemyMeleeRobot.prefab",
            component => SetReference(component, "fireCue", Cues["SFX_EnemyLaserFire"])
        );
        ConfigurePrefab<Bullet>(
            "Assets/Prefab/Player/Player_bullet.prefab",
            component =>
            {
                SetReference(component, "damageImpactCue", Cues["SFX_ImpactMetal"]);
                SetReference(component, "worldImpactCue", Cues["SFX_ImpactMetal"]);
            }
        );
        ConfigurePrefab<EnemyProjectile>(
            "Assets/Prefab/Enemy/Projectile/Enemy_bullet.prefab",
            component =>
            {
                SetReference(component, "playerImpactCue", Cues["SFX_ImpactMetal"]);
                SetReference(component, "dashAvoidedImpactCue", Cues["SFX_ShieldDeflect"]);
                SetReference(component, "worldImpactCue", Cues["SFX_ImpactMetal"]);
            }
        );

        string[] chestPrefabs =
        {
            "Assets/Loot Box/Loot Box/Model/purple box.prefab",
            "Assets/Loot Box/Loot Box/Model/blue box.prefab",
            "Assets/Loot Box/Loot Box/Model/orange box.prefab"
        };

        foreach (string path in chestPrefabs)
        {
            ConfigurePrefab<ChestInteractable>(
                path,
                component => SetReference(component, "openCue", Cues["SFX_ChestOpen"])
            );
        }
    }

    private static void ConnectCyberScene()
    {
        Scene scene = EditorSceneManager.OpenScene(CyberScenePath, OpenSceneMode.Single);

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (PlayerAimShooter shooter in root.GetComponentsInChildren<PlayerAimShooter>(true))
            {
                SetReference(shooter, "fireCue", Cues["SFX_PlayerLaserFire"]);
            }

            foreach (ChestInteractable chest in root.GetComponentsInChildren<ChestInteractable>(true))
            {
                SetReference(chest, "openCue", Cues["SFX_ChestOpen"]);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new UnityException($"Failed to save '{CyberScenePath}'.");
        }
    }

    private static void ConfigurePrefab<T>(string path, System.Action<T> configure)
        where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new MissingReferenceException($"Prefab missing: '{path}'.");
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            T[] components = contents.GetComponentsInChildren<T>(true);
            if (components.Length == 0)
            {
                throw new MissingComponentException(
                    $"Prefab '{path}' does not contain {typeof(T).Name}."
                );
            }

            foreach (T component in components)
            {
                configure(component);
            }

            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void SetReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingMemberException(
                $"'{target.GetType().Name}' is missing serialized field '{propertyName}'."
            );
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static string[] Interface(params string[] names)
    {
        return names.Select(name => $"{InterfaceFolder}/{name}.ogg").ToArray();
    }

    private static string[] SciFi(params string[] names)
    {
        return names.Select(name => $"{SciFiFolder}/{name}.ogg").ToArray();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        string name = path.Substring(separator + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
