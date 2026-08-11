using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class MeleePrototypeEditorSetup
{
    private const string ControllerPath =
        "Assets/Animation/Enemy/Melee/EnemyMeleeRobotController.controller";

    private const string AnimationPath =
        "Assets/wizardAnimation/Standing 1H Magic Attack 01.fbx";

    static MeleePrototypeEditorSetup()
    {
        EditorApplication.delayCall += RepairMissingMotion;
    }

    [MenuItem("Tools/Game/Repair Melee Prototype Animation")]
    private static void RepairFromMenu()
    {
        AssignAnimation(force: true);
    }

    private static void RepairMissingMotion()
    {
        AssignAnimation(force: false);
    }

    private static void AssignAnimation(bool force)
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null || controller.layers.Length == 0)
        {
            Debug.LogError($"Cannot load melee controller at '{ControllerPath}'.");
            return;
        }

        AnimatorState meleeState = controller.layers[0]
            .stateMachine
            .states
            .Select(childState => childState.state)
            .FirstOrDefault(state => state != null && state.name == "MeleeAttack");

        if (meleeState == null)
        {
            Debug.LogError("Cannot find the MeleeAttack state in the melee controller.");
            return;
        }

        if (!force && meleeState.motion != null)
        {
            return;
        }

        AnimationClip attackClip = AssetDatabase
            .LoadAllAssetsAtPath(AnimationPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));

        if (attackClip == null)
        {
            Debug.LogError($"Cannot find an AnimationClip inside '{AnimationPath}'.");
            return;
        }

        meleeState.motion = attackClip;
        EditorUtility.SetDirty(meleeState);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Assigned animation '{attackClip.name}' to the MeleeAttack state.",
            controller
        );
    }
}
