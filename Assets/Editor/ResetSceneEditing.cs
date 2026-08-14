using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

public static class ResetSceneEditing
{
    [MenuItem("Tools/Scene/Reset Selection And Move Tool")]
    public static void Reset()
    {
        ToolManager.RestorePreviousTool();
        Tools.current = Tool.Move;
        SceneVisibilityManager.instance.EnableAllPicking();

        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView != null)
        {
            sceneView.Focus();
            sceneView.Repaint();
        }

        Debug.Log("Scene editing reset: Move Tool selected and Scene Picking enabled for all objects.");
    }
}
