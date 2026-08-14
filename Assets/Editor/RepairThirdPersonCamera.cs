using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RepairThirdPersonCamera
{
    private const string CyberScenePath = "Assets/Scenes/CyberScene.unity";
    private const string RepairSessionKey = "RepairThirdPersonCamera_V1";
    private const string FreeLookPath = "Game/Cameras/FreeLook Camera";
    private const string MainCameraPath = "Game/Cameras/Main Camera";
    private const string TrackingTargetPath = "Game/CyberSoldier/CyberSoldierCameraTarget";

    static RepairThirdPersonCamera()
    {
        EditorApplication.delayCall += RepairAutomaticallyOnce;
    }

    private static void RepairAutomaticallyOnce()
    {
        if (SessionState.GetBool(RepairSessionKey, false) ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != CyberScenePath)
        {
            return;
        }

        Repair();
        SessionState.SetBool(RepairSessionKey, true);
    }

    [MenuItem("Tools/Camera/Repair Third Person Camera")]
    public static void Repair()
    {
        GameObject freeLookObject = GameObject.Find(FreeLookPath);
        GameObject mainCameraObject = GameObject.Find(MainCameraPath);
        GameObject trackingTargetObject = GameObject.Find(TrackingTargetPath);

        if (freeLookObject == null || mainCameraObject == null || trackingTargetObject == null)
        {
            Debug.LogError(
                "Could not repair the third-person camera. Make sure CyberScene is open and " +
                $"'{FreeLookPath}', '{MainCameraPath}', and '{TrackingTargetPath}' exist.");
            return;
        }

        CinemachineCamera freeLookCamera = freeLookObject.GetComponent<CinemachineCamera>();

        if (freeLookCamera == null)
        {
            Debug.LogError($"'{FreeLookPath}' is missing its CinemachineCamera component.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(freeLookObject, "Repair third-person camera");
        Undo.RegisterFullObjectHierarchyUndo(mainCameraObject, "Repair third-person camera");

        RemoveMisplacedBrains(freeLookObject);
        EnsureMainCameraBrain(mainCameraObject);
        RestoreTrackingTarget(freeLookCamera, trackingTargetObject.transform);

        EditorUtility.SetDirty(freeLookObject);
        EditorUtility.SetDirty(freeLookCamera);
        EditorUtility.SetDirty(mainCameraObject);
        EditorSceneManager.MarkSceneDirty(freeLookObject.scene);

        Selection.activeGameObject = freeLookObject;
        Debug.Log(
            "Third-person camera repaired: FreeLook has no CinemachineBrain, Custom Look At is disabled, " +
            "the tracking target is restored, and Main Camera owns the CinemachineBrain.");
    }

    private static void RemoveMisplacedBrains(GameObject freeLookObject)
    {
        CinemachineBrain[] misplacedBrains = freeLookObject.GetComponents<CinemachineBrain>();

        foreach (CinemachineBrain brain in misplacedBrains)
        {
            Undo.DestroyObjectImmediate(brain);
        }
    }

    private static void EnsureMainCameraBrain(GameObject mainCameraObject)
    {
        if (mainCameraObject.GetComponent<CinemachineBrain>() == null)
        {
            Undo.AddComponent<CinemachineBrain>(mainCameraObject);
        }
    }

    private static void RestoreTrackingTarget(CinemachineCamera freeLookCamera, Transform trackingTarget)
    {
        SerializedObject serializedCamera = new SerializedObject(freeLookCamera);
        SerializedProperty target = serializedCamera.FindProperty("Target");

        if (target == null)
        {
            Debug.LogError("Could not find the CinemachineCamera Target property.");
            return;
        }

        SerializedProperty trackingTargetProperty = target.FindPropertyRelative("TrackingTarget");
        SerializedProperty lookAtTargetProperty = target.FindPropertyRelative("LookAtTarget");
        SerializedProperty customLookAtProperty = target.FindPropertyRelative("CustomLookAtTarget");

        trackingTargetProperty.objectReferenceValue = trackingTarget;
        lookAtTargetProperty.objectReferenceValue = null;
        customLookAtProperty.boolValue = false;
        serializedCamera.ApplyModifiedProperties();
    }
}
