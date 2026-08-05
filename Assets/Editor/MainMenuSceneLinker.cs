using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MainMenuSceneLinker
{
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string CyberScenePath = "Assets/Scenes/CyberScene.unity";

    static MainMenuSceneLinker()
    {
        EditorApplication.delayCall += AutoWireOpenMainMenu;
    }

    private static void AutoWireOpenMainMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject canvasObject = GameObject.Find("MainMenuCanvas");

        if (scene.path == MainMenuScenePath && canvasObject != null && canvasObject.GetComponent<MainMenuUI>() == null)
        {
            WireMainMenuNavigation();
        }
    }

    [MenuItem("Tools/UI/Wire Main Menu Navigation")]
    public static void WireMainMenuNavigation()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != MainMenuScenePath)
        {
            EditorUtility.DisplayDialog(
                "Wrong Scene",
                $"Open '{MainMenuScenePath}' before wiring the Main Menu. The current scene is '{scene.path}'.",
                "OK"
            );
            return;
        }

        GameObject canvasObject = GameObject.Find("MainMenuCanvas");

        if (canvasObject == null)
        {
            EditorUtility.DisplayDialog(
                "Missing Hierarchy",
                "MainMenuCanvas was not found. Build the Main Menu hierarchy first.",
                "OK"
            );
            return;
        }

        Transform canvas = canvasObject.transform;
        GameObject mainMenuRoot = FindRequired(canvas, "MainMenuRoot");
        GameObject settingsPanel = FindRequired(canvas, "SettingsPanel");
        GameObject loadingOverlay = FindRequired(canvas, "LoadingOverlay");
        Button playButton = FindRequiredComponent<Button>(canvas, "MainMenuRoot/MenuButtonContainer/PlayButton");
        Button settingsButton = FindRequiredComponent<Button>(canvas, "MainMenuRoot/MenuButtonContainer/SettingsButton");
        Button quitButton = FindRequiredComponent<Button>(canvas, "MainMenuRoot/MenuButtonContainer/QuitButton");
        Button backButton = FindRequiredComponent<Button>(canvas, "SettingsPanel/SettingsWindow/Footer/BackButton");

        if (mainMenuRoot == null || settingsPanel == null || loadingOverlay == null || playButton == null ||
            settingsButton == null || quitButton == null || backButton == null)
        {
            return;
        }

        MainMenuUI mainMenuUI = canvasObject.GetComponent<MainMenuUI>();

        if (mainMenuUI == null)
        {
            mainMenuUI = Undo.AddComponent<MainMenuUI>(canvasObject);
        }

        SerializedObject serializedMenu = new SerializedObject(mainMenuUI);
        serializedMenu.FindProperty("mainMenuRoot").objectReferenceValue = mainMenuRoot;
        serializedMenu.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        serializedMenu.FindProperty("loadingOverlay").objectReferenceValue = loadingOverlay;
        serializedMenu.FindProperty("playButton").objectReferenceValue = playButton;
        serializedMenu.FindProperty("settingsButton").objectReferenceValue = settingsButton;
        serializedMenu.FindProperty("quitButton").objectReferenceValue = quitButton;
        serializedMenu.FindProperty("settingsBackButton").objectReferenceValue = backButton;
        serializedMenu.ApplyModifiedProperties();

        EnsureBuildSceneOrder();
        EditorUtility.SetDirty(mainMenuUI);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = canvasObject;
        EditorGUIUtility.PingObject(canvasObject);

        EditorUtility.DisplayDialog(
            "Main Menu Wired",
            "Play now loads CyberScene. Settings opens the SettingsPanel, Back returns to the Main Menu, and the build scene order was updated.",
            "OK"
        );
    }

    private static GameObject FindRequired(Transform root, string path)
    {
        Transform result = root.Find(path);

        if (result == null)
        {
            Debug.LogError($"Main Menu hierarchy is missing '{path}'.");
            return null;
        }

        return result.gameObject;
    }

    private static T FindRequiredComponent<T>(Transform root, string path) where T : Component
    {
        GameObject gameObject = FindRequired(root, path);

        if (gameObject == null)
        {
            return null;
        }

        T component = gameObject.GetComponent<T>();

        if (component == null)
        {
            Debug.LogError($"'{path}' is missing component {typeof(T).Name}.", gameObject);
        }

        return component;
    }

    private static void EnsureBuildSceneOrder()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(CyberScenePath, true)
        };

        foreach (EditorBuildSettingsScene existingScene in EditorBuildSettings.scenes)
        {
            if (existingScene.path == MainMenuScenePath || existingScene.path == CyberScenePath)
            {
                continue;
            }

            scenes.Add(existingScene);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
