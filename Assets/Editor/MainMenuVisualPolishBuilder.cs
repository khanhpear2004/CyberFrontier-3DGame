using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainMenuVisualPolishBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string PanelSpritePath =
        "Assets/UIAssets/Assets Wenrexa UI Sci-Fi Minimalism #01/Elements/BlockInformation.png";

    [MenuItem("Tools/UI/Bake Main Menu Visual Polish")]
    public static void BakeOpenMainMenu()
    {
        BakeScene(SceneManager.GetActiveScene(), true);
    }

    [MenuItem("Tools/UI/Bake Main Menu Visual Polish", true)]
    private static bool ValidateBakeOpenMainMenu()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               SceneManager.GetActiveScene().path == MainMenuScenePath;
    }

    public static void BakeFromCommandLine()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        BakeScene(scene, false);
    }

    private static void BakeScene(Scene scene, bool selectResult)
    {
        if (!scene.IsValid() || scene.path != MainMenuScenePath)
        {
            Debug.LogError($"Open '{MainMenuScenePath}' before baking Main Menu visual polish.");
            return;
        }

        GameObject root = GameObject.Find("MainMenuCanvas/MainMenuRoot");
        if (root == null)
        {
            Debug.LogError("MainMenuCanvas/MainMenuRoot was not found.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Bake Main Menu Visual Polish");

        MainMenuVisualPolish polish = root.GetComponent<MainMenuVisualPolish>();
        if (polish == null)
        {
            polish = Undo.AddComponent<MainMenuVisualPolish>(root);
        }

        SerializedObject serializedPolish = new(polish);
        SerializedProperty panelSprite = serializedPolish.FindProperty("panelSprite");
        if (panelSprite.objectReferenceValue == null)
        {
            panelSprite.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
            serializedPolish.ApplyModifiedPropertiesWithoutUndo();
        }

        polish.BakeIntoScene();
        Undo.DestroyObjectImmediate(polish);

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (selectResult)
        {
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }

        Debug.Log(
            "Main Menu visual polish was baked into the scene. " +
            "The runtime MainMenuVisualPolish component was removed.",
            root);
    }
}
