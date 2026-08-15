using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class AudioSystemBuilder
{
    private const BindingFlags AllFlags =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private const string MixerPath = "Assets/Audio/Mixers/MainAudioMixer.mixer";
    private const string CoreSystemsPath = "Assets/Resources/CoreSystems.prefab";
    private const string CueFolder = "Assets/Audio/Data/Cues";
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string CityTrafficPath =
        "Assets/_DLNK/Neon City/Scene/NeonScene00/Source/City_traffic.mp3";
    private const string StormPath =
        "Assets/_DLNK/Neon City/Scene/NeonScene00/Source/Storm.mp3";

    private static readonly string[] ChildGroupNames = { "Music", "SFX", "UI", "Ambience" };

    [MenuItem("Tools/Audio/Build Audio Core")]
    public static void BuildFromMenu()
    {
        Build(selectCreatedAsset: true);
    }

    public static void Build(bool selectCreatedAsset)
    {
        EnsureFolder("Assets/Audio");
        EnsureFolder("Assets/Audio/Mixers");
        EnsureFolder("Assets/Audio/Data");
        EnsureFolder(CueFolder);
        EnsureFolder("Assets/Resources");

        AudioMixer mixer = CreateOrUpdateMixer();
        Dictionary<string, AudioMixerGroup> groups = GetRequiredGroups(mixer);
        CreateCoreSystemsPrefab(mixer, groups);
        CreateExampleCue(
            $"{CueFolder}/AMB_CityTraffic.asset",
            CityTrafficPath,
            groups["Ambience"],
            0.35f,
            0.5f
        );
        CreateExampleCue(
            $"{CueFolder}/AMB_Storm.asset",
            StormPath,
            groups["Ambience"],
            0.25f,
            0.4f
        );
        AddAudioFeedbackToButtonPrefabs();
        AudioCue mainMenuAmbience = AssetDatabase.LoadAssetAtPath<AudioCue>(
            $"{CueFolder}/AMB_CityTraffic.asset"
        );
        ConnectMainMenuSettings(mixer, mainMenuAmbience);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (selectCreatedAsset)
        {
            Selection.activeObject = mixer;
            EditorGUIUtility.PingObject(mixer);
        }

        Debug.Log(
            "Audio core built: MainAudioMixer, CoreSystems, AudioCue examples and live Settings sliders."
        );
    }

    private static AudioMixer CreateOrUpdateMixer()
    {
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        Type controllerType = Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");
        Type groupControllerType = Type.GetType(
            "UnityEditor.Audio.AudioMixerGroupController, UnityEditor"
        );

        if (controllerType == null || groupControllerType == null)
        {
            throw new InvalidOperationException("Unity AudioMixer editor API was not found.");
        }

        if (mixer == null)
        {
            MethodInfo createMethod = controllerType.GetMethod(
                "CreateMixerControllerAtPath",
                AllFlags
            );
            mixer = createMethod?.Invoke(null, new object[] { MixerPath }) as AudioMixer;
        }

        if (mixer == null)
        {
            throw new UnityException($"Failed to create '{MixerPath}'.");
        }

        PropertyInfo masterProperty = controllerType.GetProperty("masterGroup", AllFlags);
        object masterGroup = masterProperty?.GetValue(mixer);
        MethodInfo createGroupMethod = controllerType.GetMethod("CreateNewGroup", AllFlags);
        MethodInfo addChildMethod = controllerType.GetMethod("AddChildToParent", AllFlags);

        foreach (string groupName in ChildGroupNames)
        {
            bool exists = mixer.FindMatchingGroups(groupName).Any(group => group.name == groupName);
            if (exists)
            {
                continue;
            }

            object child = createGroupMethod?.Invoke(mixer, new object[] { groupName, false });
            addChildMethod?.Invoke(mixer, new[] { child, masterGroup });
        }

        AssetDatabase.SaveAssets();
        Dictionary<string, AudioMixerGroup> groups = GetRequiredGroups(mixer);
        ExposeGroupVolume(mixer, groups["Master"], AudioManager.MasterVolumeParameter);
        ExposeGroupVolume(mixer, groups["Music"], AudioManager.MusicVolumeParameter);
        ExposeGroupVolume(mixer, groups["SFX"], AudioManager.SfxVolumeParameter);
        ExposeGroupVolume(mixer, groups["UI"], AudioManager.UIVolumeParameter);
        ExposeGroupVolume(mixer, groups["Ambience"], AudioManager.AmbienceVolumeParameter);

        EditorUtility.SetDirty(mixer);
        AssetDatabase.SaveAssets();
        return mixer;
    }

    private static void ExposeGroupVolume(
        AudioMixer mixer,
        AudioMixerGroup group,
        string parameterName
    )
    {
        Type controllerType = Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");
        Type groupControllerType = Type.GetType(
            "UnityEditor.Audio.AudioMixerGroupController, UnityEditor"
        );
        Type exposedType = Type.GetType("UnityEditor.Audio.ExposedAudioParameter, UnityEditor");
        PropertyInfo exposedProperty = controllerType?.GetProperty("exposedParameters", AllFlags);
        MethodInfo guidMethod = groupControllerType?.GetMethod("GetGUIDForVolume", AllFlags);
        FieldInfo guidField = exposedType?.GetField("guid", AllFlags);
        FieldInfo nameField = exposedType?.GetField("name", AllFlags);

        if (exposedProperty == null || guidMethod == null || guidField == null || nameField == null)
        {
            throw new MissingMemberException("Could not access AudioMixer exposed parameters.");
        }

        object volumeGuid = guidMethod.Invoke(group, null);
        Array existing = (Array)exposedProperty.GetValue(mixer);
        List<object> parameters = existing.Cast<object>().ToList();
        object matching = parameters.FirstOrDefault(item => Equals(guidField.GetValue(item), volumeGuid));

        if (matching != null)
        {
            nameField.SetValue(matching, parameterName);
        }
        else
        {
            object exposed = Activator.CreateInstance(exposedType);
            guidField.SetValue(exposed, volumeGuid);
            nameField.SetValue(exposed, parameterName);
            parameters.Add(exposed);
        }

        Array updated = Array.CreateInstance(exposedType, parameters.Count);
        for (int index = 0; index < parameters.Count; index++)
        {
            updated.SetValue(parameters[index], index);
        }

        exposedProperty.SetValue(mixer, updated);
    }

    private static Dictionary<string, AudioMixerGroup> GetRequiredGroups(AudioMixer mixer)
    {
        Dictionary<string, AudioMixerGroup> groups = mixer
            .FindMatchingGroups(string.Empty)
            .GroupBy(group => group.name)
            .ToDictionary(group => group.Key, group => group.First());

        string[] required = { "Master", "Music", "SFX", "UI", "Ambience" };
        foreach (string name in required)
        {
            if (!groups.ContainsKey(name))
            {
                throw new MissingReferenceException($"AudioMixer group '{name}' is missing.");
            }
        }

        return groups;
    }

    private static void CreateCoreSystemsPrefab(
        AudioMixer mixer,
        IReadOnlyDictionary<string, AudioMixerGroup> groups
    )
    {
        GameObject root = new GameObject("CoreSystems", typeof(AudioManager));

        try
        {
            AudioSource musicA = CreateDedicatedSource("MusicSourceA", root.transform, groups["Music"]);
            AudioSource musicB = CreateDedicatedSource("MusicSourceB", root.transform, groups["Music"]);
            AudioSource ambience = CreateDedicatedSource(
                "AmbienceSource",
                root.transform,
                groups["Ambience"]
            );

            SerializedObject manager = new SerializedObject(root.GetComponent<AudioManager>());
            manager.FindProperty("mixer").objectReferenceValue = mixer;
            manager.FindProperty("musicGroup").objectReferenceValue = groups["Music"];
            manager.FindProperty("sfxGroup").objectReferenceValue = groups["SFX"];
            manager.FindProperty("uiGroup").objectReferenceValue = groups["UI"];
            manager.FindProperty("ambienceGroup").objectReferenceValue = groups["Ambience"];
            manager.FindProperty("musicSourceA").objectReferenceValue = musicA;
            manager.FindProperty("musicSourceB").objectReferenceValue = musicB;
            manager.FindProperty("ambienceSource").objectReferenceValue = ambience;
            manager.FindProperty("pooledSourceCount").intValue = 24;
            manager.ApplyModifiedPropertiesWithoutUndo();

            if (PrefabUtility.SaveAsPrefabAsset(root, CoreSystemsPath) == null)
            {
                throw new UnityException($"Failed to create '{CoreSystemsPath}'.");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static AudioSource CreateDedicatedSource(
        string objectName,
        Transform parent,
        AudioMixerGroup output
    )
    {
        GameObject sourceObject = new GameObject(objectName, typeof(AudioSource));
        sourceObject.transform.SetParent(parent, false);
        AudioSource source = sourceObject.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = output;
        return source;
    }

    private static void CreateExampleCue(
        string assetPath,
        string clipPath,
        AudioMixerGroup output,
        float minimumVolume,
        float maximumVolume
    )
    {
        AudioCue cue = AssetDatabase.LoadAssetAtPath<AudioCue>(assetPath);
        if (cue == null)
        {
            cue = ScriptableObject.CreateInstance<AudioCue>();
            AssetDatabase.CreateAsset(cue, assetPath);
        }

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        SerializedObject serializedCue = new SerializedObject(cue);
        SerializedProperty clips = serializedCue.FindProperty("clips");
        clips.arraySize = clip != null ? 1 : 0;
        if (clip != null)
        {
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
        }

        serializedCue.FindProperty("bus").enumValueIndex = (int)AudioBus.Ambience;
        serializedCue.FindProperty("outputOverride").objectReferenceValue = output;
        serializedCue.FindProperty("volumeRange").vector2Value =
            new Vector2(minimumVolume, maximumVolume);
        serializedCue.FindProperty("pitchRange").vector2Value = new Vector2(0.98f, 1.02f);
        serializedCue.FindProperty("loop").boolValue = true;
        serializedCue.FindProperty("spatialBlend").floatValue = 0f;
        serializedCue.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cue);
    }

    private static void ConnectMainMenuSettings(AudioMixer mixer, AudioCue mainMenuAmbience)
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        GameObject settingsManager = GameObject.Find("GameSettingsManager");
        GameObject canvas = GameObject.Find("MainMenuCanvas");

        if (settingsManager == null || canvas == null)
        {
            throw new MissingReferenceException(
                "GameSettingsManager or MainMenuCanvas was not found in Main Menu."
            );
        }

        Transform content = canvas.transform.Find(
            "SettingsPanel/SettingsWindow/SettingsContent"
        );
        Slider master = FindRowSlider(content, "MasterVolumeRow");
        Slider music = FindRowSlider(content, "MusicVolumeRow");
        Slider sfx = FindRowSlider(content, "SfxVolumeRow");

        AudioSettingsController controller =
            settingsManager.GetComponent<AudioSettingsController>() ??
            settingsManager.AddComponent<AudioSettingsController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("mixer").objectReferenceValue = mixer;
        serializedController.FindProperty("masterVolumeSlider").objectReferenceValue = master;
        serializedController.FindProperty("musicVolumeSlider").objectReferenceValue = music;
        serializedController.FindProperty("sfxVolumeSlider").objectReferenceValue = sfx;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);

        MainMenuAudioController menuAudio =
            settingsManager.GetComponent<MainMenuAudioController>() ??
            settingsManager.AddComponent<MainMenuAudioController>();
        SerializedObject serializedMenuAudio = new SerializedObject(menuAudio);
        serializedMenuAudio.FindProperty("ambienceCue").objectReferenceValue = mainMenuAmbience;
        serializedMenuAudio.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(menuAudio);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new UnityException($"Failed to save '{MainMenuScenePath}'.");
        }
    }

    private static Slider FindRowSlider(Transform content, string rowName)
    {
        Slider slider = content?.Find($"{rowName}/Slider")?.GetComponent<Slider>();
        if (slider == null)
        {
            throw new MissingReferenceException($"Settings slider '{rowName}' was not found.");
        }

        return slider;
    }

    private static void AddAudioFeedbackToButtonPrefabs()
    {
        string[] paths =
        {
            "Assets/UI/Prefabs/Common/SciFiPrimaryButton.prefab",
            "Assets/UI/Prefabs/Common/SciFiSecondaryButton.prefab",
            "Assets/UI/Prefabs/Common/SciFiBackButton.prefab"
        };

        foreach (string path in paths)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(path))
            {
                continue;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (contents.GetComponent<UIAudioFeedback>() == null)
                {
                    contents.AddComponent<UIAudioFeedback>();
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = path.Substring(0, path.LastIndexOf('/'));
        string name = path.Substring(path.LastIndexOf('/') + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
