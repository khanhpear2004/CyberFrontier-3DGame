using System.Linq;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.Splines;
using Unity.Mathematics;
using Unity.Cinemachine;

[InitializeOnLoad]
public static class IntroCutsceneDollyTimelineSetup
{
    private const string CyberScenePath = "Assets/Scenes/CyberScene.unity";
    private const string TimelinePath = "Assets/Timeline/Intro/GameplayIntro.playable";
    private const string AutoSetupSessionKey = "IntroCutsceneDollyTimelineSetup_V9";

    private const float EstablishEnd = 5f;
    private const float StreetEnd = 9f;
    private const float NeonEnd = 13f;
    private const float ThreatEnd = 17.5f;
    private const float PlayerEnd = 28f;
    private const float CutsceneEnd = 31.5f;

    static IntroCutsceneDollyTimelineSetup()
    {
        EditorApplication.delayCall += TryRunAutomaticSetup;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += TryRunAutomaticSetup;
        }
    }

    [MenuItem("Tools/Cutscene/Build Extended City Intro")]
    public static void RepairIntroCameraDollyTracks()
    {
        if (!TrySetup(showDialog: true))
        {
            return;
        }

        SessionState.SetBool(AutoSetupSessionKey, true);
    }

    private static void TryRunAutomaticSetup()
    {
        if (SessionState.GetBool(AutoSetupSessionKey, false) ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
        {
            return;
        }

        if (SceneManager.GetActiveScene().path != CyberScenePath)
        {
            return;
        }

        if (TrySetup(showDialog: false))
        {
            SessionState.SetBool(AutoSetupSessionKey, true);
            Debug.Log("Intro cutscene timing, fade transitions, and dolly tracks were rebuilt with Unity Timeline API.");
        }
    }

    private static bool TrySetup(bool showDialog)
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != CyberScenePath)
        {
            ShowError(showDialog, $"Open '{CyberScenePath}' before repairing the cutscene tracks.");
            return false;
        }

        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
        GameObject introSequence = GameObject.Find("Game/Systems/IntroSequence");
        GameObject establishCamera = GameObject.Find("Game/Cameras/IntroEstablishCamera");
        GameObject streetCamera = GameObject.Find("Game/Cameras/IntroStreetCamera");
        GameObject neonCamera = GameObject.Find("Game/Cameras/IntroNeonCamera");
        GameObject threatCamera = GameObject.Find("Game/Cameras/IntroThreatCamera");
        GameObject playerCamera = GameObject.Find("Game/Cameras/IntroPlayerCamera");
        GameObject freeLookCamera = GameObject.Find("Game/Cameras/FreeLook Camera");
        GameObject player = GameObject.Find("Game/CyberSoldier");
        GameObject gameplayCameraTarget = GameObject.Find("Game/CyberSoldier/CyberSoldierCameraTarget");
        GameObject playerRevealPath = GameObject.Find("Game/CutscenePaths/PlayerRevealPath");

        if (timeline == null || introSequence == null || establishCamera == null ||
            threatCamera == null || playerCamera == null || freeLookCamera == null || player == null ||
            gameplayCameraTarget == null || playerRevealPath == null)
        {
            ShowError(showDialog, "GameplayIntro, IntroSequence, or one of the intro cameras could not be found.");
            return false;
        }

        PlayableDirector director = introSequence.GetComponent<PlayableDirector>();

        if (director == null)
        {
            ShowError(showDialog, "IntroSequence is missing its PlayableDirector.");
            return false;
        }

        Undo.RegisterCompleteObjectUndo(timeline, "Repair intro camera dolly tracks");
        streetCamera = CreateCityContextShot(
            "IntroStreetCamera",
            "StreetContextPath",
            "StreetContextTarget",
            new Vector3(8f, 1.5f, 2f),
            new[]
            {
                new Vector3(-8f, 4.5f, -4f),
                new Vector3(-2f, 3.5f, -2f),
                new Vector3(5f, 3f, 0f),
                new Vector3(12f, 3.2f, 2f)
            },
            48f
        );

        neonCamera = CreateCityContextShot(
            "IntroNeonCamera",
            "NeonContextPath",
            "NeonContextTarget",
            new Vector3(12f, 6f, 5f),
            new[]
            {
                new Vector3(2f, 12f, 14f),
                new Vector3(7f, 10f, 10f),
                new Vector3(13f, 8f, 7f),
                new Vector3(18f, 6f, 4f)
            },
            45f
        );

        if (streetCamera == null || neonCamera == null)
        {
            ShowError(showDialog, "The two city context cameras could not be created.");
            return false;
        }

        ConfigureAllCameraShots(
            timeline,
            director,
            establishCamera,
            streetCamera,
            neonCamera,
            threatCamera,
            playerCamera,
            freeLookCamera
        );
        ConfigureActivationClips(timeline);
        ConfigureFadeOverlay(timeline);
        ConfigurePlayerRevealCamera(
            playerCamera,
            gameplayCameraTarget,
            player,
            playerRevealPath
        );
        RemoveTrackIfPresent(timeline, "Threat Dolly Movement");
        RemoveTrackIfPresent(timeline, "Player Reveal Movement");
        RemoveTrackIfPresent(timeline, "Street Context Movement");
        RemoveTrackIfPresent(timeline, "Neon Context Movement");

        Animator establishAnimator = GetOrAddAnimator(establishCamera);
        Animator threatAnimator = GetOrAddAnimator(threatCamera);
        Animator playerAnimator = GetOrAddAnimator(playerCamera);
        Animator streetAnimator = GetOrAddAnimator(streetCamera);
        Animator neonAnimator = GetOrAddAnimator(neonCamera);

        ConfigureExistingDollyTrack(
            timeline,
            director,
            establishAnimator,
            0f,
            4.5f
        );

        AnimationTrack threatTrack = CreateDollyTrack(
            timeline,
            "Threat Dolly Movement",
            "Threat Dolly Recorded",
            13f,
            17f
        );

        AnimationTrack playerTrack = CreateDollyTrack(
            timeline,
            "Player Reveal Movement",
            "Player Reveal Recorded",
            17.5f,
            28f
        );

        ConfigurePlayerRevealMovement(
            playerTrack
        );

        AnimationTrack streetTrack = CreateDollyTrack(
            timeline,
            "Street Context Movement",
            "Street Context Recorded",
            5f,
            8.5f
        );

        AnimationTrack neonTrack = CreateDollyTrack(
            timeline,
            "Neon Context Movement",
            "Neon Context Recorded",
            9f,
            12.5f
        );

        director.SetGenericBinding(threatTrack, threatAnimator);
        director.SetGenericBinding(playerTrack, playerAnimator);
        director.SetGenericBinding(streetTrack, streetAnimator);
        director.SetGenericBinding(neonTrack, neonAnimator);
        director.RebuildGraph();

        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(director);
        EditorUtility.SetDirty(threatAnimator);
        EditorUtility.SetDirty(playerAnimator);
        EditorUtility.SetDirty(streetAnimator);
        EditorUtility.SetDirty(neonAnimator);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Extended City Intro Built",
                "Two city context shots were inserted between Establish and Threat. " +
                "The six camera shots are separated by five fade-to-black transitions over 31.5 seconds. " +
                "The player reveal now uses a slower elevated orbit and a hero close-up.",
                "OK"
            );
        }

        return true;
    }

    private static GameObject CreateCityContextShot(
        string cameraName,
        string pathName,
        string targetName,
        Vector3 targetPosition,
        Vector3[] pathPoints,
        float fieldOfView)
    {
        Transform camerasParent = GameObject.Find("Game/Cameras")?.transform;
        Transform pathsParent = GameObject.Find("Game/CutscenePaths")?.transform;
        Transform targetsParent = GameObject.Find("Game/CutsceneTargets")?.transform;

        if (camerasParent == null || pathsParent == null || targetsParent == null)
        {
            return null;
        }

        GameObject target = GameObject.Find($"Game/CutsceneTargets/{targetName}");

        if (target == null)
        {
            target = new GameObject(targetName);
            Undo.RegisterCreatedObjectUndo(target, $"Create {targetName}");
            target.transform.SetParent(targetsParent, false);
            target.transform.position = targetPosition;
        }

        GameObject pathObject = GameObject.Find($"Game/CutscenePaths/{pathName}");

        if (pathObject == null)
        {
            pathObject = new GameObject(pathName);
            Undo.RegisterCreatedObjectUndo(pathObject, $"Create {pathName}");
            pathObject.transform.SetParent(pathsParent, false);
        }

        SplineContainer path = pathObject.GetComponent<SplineContainer>();

        if (path == null)
        {
            path = Undo.AddComponent<SplineContainer>(pathObject);
        }

        if (path.Spline.Count == 0)
        {
            foreach (Vector3 worldPoint in pathPoints)
            {
                Vector3 localPoint = path.transform.InverseTransformPoint(worldPoint);
                path.Spline.Add(new float3(localPoint.x, localPoint.y, localPoint.z), TangentMode.AutoSmooth);
            }
        }

        GameObject cameraObject = GameObject.Find($"Game/Cameras/{cameraName}");

        if (cameraObject == null)
        {
            cameraObject = new GameObject(cameraName);
            Undo.RegisterCreatedObjectUndo(cameraObject, $"Create {cameraName}");
            cameraObject.transform.SetParent(camerasParent, false);
        }

        CinemachineCamera camera = cameraObject.GetComponent<CinemachineCamera>();

        if (camera == null)
        {
            camera = Undo.AddComponent<CinemachineCamera>(cameraObject);
        }

        CinemachineSplineDolly dolly = cameraObject.GetComponent<CinemachineSplineDolly>();

        if (dolly == null)
        {
            dolly = Undo.AddComponent<CinemachineSplineDolly>(cameraObject);
        }

        if (cameraObject.GetComponent<CinemachineHardLookAt>() == null)
        {
            Undo.AddComponent<CinemachineHardLookAt>(cameraObject);
        }

        camera.Target.TrackingTarget = target.transform;
        camera.Target.LookAtTarget = null;
        camera.Target.CustomLookAtTarget = false;
        camera.Lens.FieldOfView = fieldOfView;
        camera.Priority = 0;
        dolly.Spline = path;
        dolly.CameraPosition = 0f;
        dolly.CameraRotation = CinemachineSplineDolly.RotationMode.Default;
        dolly.Damping.Enabled = false;

        EditorUtility.SetDirty(path);
        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(dolly);
        return cameraObject;
    }

    private static void ConfigureAllCameraShots(
        TimelineAsset timeline,
        PlayableDirector director,
        GameObject establishCameraObject,
        GameObject streetCameraObject,
        GameObject neonCameraObject,
        GameObject threatCameraObject,
        GameObject playerCameraObject,
        GameObject freeLookCameraObject)
    {
        CinemachineTrack cameraTrack = timeline.GetOutputTracks().OfType<CinemachineTrack>().FirstOrDefault();

        if (cameraTrack == null)
        {
            Debug.LogWarning("The Cinemachine Timeline track was not found.");
            return;
        }

        ConfigureCameraShot(cameraTrack, director, establishCameraObject.GetComponent<CinemachineCamera>(),
            "IntroEstablishCamera", 0d, EstablishEnd);
        ConfigureCameraShot(cameraTrack, director, streetCameraObject.GetComponent<CinemachineCamera>(),
            "IntroStreetCamera", EstablishEnd, StreetEnd - EstablishEnd);
        ConfigureCameraShot(cameraTrack, director, neonCameraObject.GetComponent<CinemachineCamera>(),
            "IntroNeonCamera", StreetEnd, NeonEnd - StreetEnd);
        ConfigureCameraShot(cameraTrack, director, threatCameraObject.GetComponent<CinemachineCamera>(),
            "IntroThreatCamera", NeonEnd, ThreatEnd - NeonEnd);
        ConfigureCameraShot(cameraTrack, director, playerCameraObject.GetComponent<CinemachineCamera>(),
            "IntroPlayerCamera", ThreatEnd, PlayerEnd - ThreatEnd);
        ConfigureCameraShot(cameraTrack, director, freeLookCameraObject.GetComponent<CinemachineCamera>(),
            "FreeLook Camera", PlayerEnd, CutsceneEnd - PlayerEnd);
    }

    private static void ConfigureCameraShot(
        CinemachineTrack track,
        PlayableDirector director,
        CinemachineCamera camera,
        string displayName,
        double start,
        double duration)
    {
        TimelineClip clip = track.GetClips().FirstOrDefault(candidate => candidate.displayName == displayName);

        if (clip == null)
        {
            clip = track.CreateClip<CinemachineShot>();
        }

        clip.displayName = displayName;
        clip.start = start;
        clip.duration = duration;
        clip.blendInDuration = 0d;
        clip.blendOutDuration = 0d;

        CinemachineShot shot = clip.asset as CinemachineShot;

        if (shot == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(shot.VirtualCamera.exposedName.ToString()))
        {
            shot.VirtualCamera.exposedName = new PropertyName(Guid.NewGuid().ToString("N"));
        }

        shot.DisplayName = displayName;
        director.SetReferenceValue(shot.VirtualCamera.exposedName, camera);
        EditorUtility.SetDirty(shot);
    }

    private static void ConfigurePlayerRevealCamera(
        GameObject playerCameraObject,
        GameObject gameplayCameraTarget,
        GameObject player,
        GameObject playerRevealPath)
    {
        CinemachineCamera playerCamera = playerCameraObject.GetComponent<CinemachineCamera>();
        SplineContainer path = playerRevealPath.GetComponent<SplineContainer>();
        PlayerRevealPathSettings settings = playerRevealPath.GetComponent<PlayerRevealPathSettings>();

        if (settings == null)
        {
            settings = Undo.AddComponent<PlayerRevealPathSettings>(playerRevealPath);
        }

        if (playerCamera == null || path == null || settings == null)
        {
            Debug.LogWarning("The player reveal camera could not be configured because a required component is missing.");
            return;
        }

        Undo.RecordObject(playerCamera, "Aim intro camera at player");
        playerCamera.Target.TrackingTarget = gameplayCameraTarget.transform;
        playerCamera.Target.LookAtTarget = null;
        playerCamera.Target.CustomLookAtTarget = false;

        ExtendPlayerRevealSpline(path, player.transform, gameplayCameraTarget.transform, settings);

        EditorUtility.SetDirty(playerCamera);
    }

    private static void ExtendPlayerRevealSpline(
        SplineContainer path,
        Transform player,
        Transform cameraTarget,
        PlayerRevealPathSettings settings)
    {
        Spline spline = path.Spline;

        const int approachKnotCount = 4;

        // Preserve the four hand-authored approach knots and rebuild only the orbit.
        if (spline.Count < approachKnotCount)
        {
            Debug.LogWarning(
                $"{path.name} needs at least {approachKnotCount} approach knots before its orbit can be generated.",
                path);
            return;
        }

        Undo.RecordObject(path, "Rebuild player reveal camera orbit");

        while (spline.Count > approachKnotCount)
        {
            spline.RemoveAt(spline.Count - 1);
        }

        Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 target = cameraTarget.position;
        const float orbitStartAngle = -38f;
        const float orbitEndAngle = -180f;
        const int orbitKnotCount = 10;
        var orbitPoints = new List<Vector3>(orbitKnotCount);

        for (int index = 0; index < orbitKnotCount; index++)
        {
            float progress = index / (orbitKnotCount - 1f);
            float angle = Mathf.Lerp(orbitStartAngle, orbitEndAngle, progress) * Mathf.Deg2Rad;
            float zoomIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.72f, progress));
            float zoomOut = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, progress));
            float orbitRadius = Mathf.Lerp(settings.OrbitRadius, settings.CloseUpRadius, zoomIn);
            orbitRadius = Mathf.Lerp(orbitRadius, settings.OrbitRadius, zoomOut);
            Vector3 orbitOffset =
                forward * (Mathf.Cos(angle) * orbitRadius) +
                right * (Mathf.Sin(angle) * orbitRadius);
            float cameraHeight =
                settings.OrbitBaseHeight +
                Mathf.Sin(progress * Mathf.PI) * settings.OrbitHeightLift;
            Vector3 worldPoint = target + orbitOffset + Vector3.up * cameraHeight;
            Vector3 localPoint = path.transform.InverseTransformPoint(worldPoint);
            orbitPoints.Add(localPoint);
        }

        for (int index = 0; index < orbitPoints.Count; index++)
        {
            Vector3 previousPoint;
            if (index == 0)
            {
                BezierKnot previousKnot = spline[approachKnotCount - 1];
                previousPoint = new Vector3(
                    previousKnot.Position.x,
                    previousKnot.Position.y,
                    previousKnot.Position.z);
            }
            else
            {
                previousPoint = orbitPoints[index - 1];
            }

            Vector3 nextPoint = index == orbitPoints.Count - 1
                ? orbitPoints[index] + (orbitPoints[index] - previousPoint)
                : orbitPoints[index + 1];
            Vector3 localTangent = (nextPoint - previousPoint) / 6f;
            BezierKnot orbitKnot = new(
                new float3(orbitPoints[index].x, orbitPoints[index].y, orbitPoints[index].z),
                new float3(-localTangent.x, -localTangent.y, -localTangent.z),
                new float3(localTangent.x, localTangent.y, localTangent.z),
                quaternion.identity);
            spline.Add(orbitKnot, TangentMode.Broken);
        }

        EditorUtility.SetDirty(path);
    }

    private static void ConfigureCameraShots(TimelineAsset timeline)
    {
        Dictionary<string, (double Start, double Duration)> timing = new()
        {
            { "IntroEstablishCamera", (0d, EstablishEnd) },
            { "IntroStreetCamera", (EstablishEnd, StreetEnd - EstablishEnd) },
            { "IntroNeonCamera", (StreetEnd, NeonEnd - StreetEnd) },
            { "IntroThreatCamera", (NeonEnd, ThreatEnd - NeonEnd) },
            { "IntroPlayerCamera", (ThreatEnd, PlayerEnd - ThreatEnd) },
            { "FreeLook Camera", (PlayerEnd, CutsceneEnd - PlayerEnd) }
        };

        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            foreach (TimelineClip clip in track.GetClips())
            {
                if (!timing.TryGetValue(clip.displayName, out var values))
                {
                    continue;
                }

                clip.start = values.Start;
                clip.duration = values.Duration;
                clip.blendInDuration = 0d;
                clip.blendOutDuration = 0d;
            }
        }
    }

    private static void ConfigureActivationClips(TimelineAsset timeline)
    {
        Dictionary<string, (double Start, double Duration)> timing = new()
        {
            { "Sub01", (1d, 3d) },
            { "Sub02", (13.5d, 3d) },
            { "Sub03", (19d, 3d) },
            { "Objective", (28.5d, 3d) }
        };

        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            foreach (TimelineClip clip in track.GetClips())
            {
                if (!timing.TryGetValue(clip.displayName, out var values))
                {
                    continue;
                }

                clip.start = values.Start;
                clip.duration = values.Duration;
            }
        }
    }

    private static void ConfigureFadeOverlay(TimelineAsset timeline)
    {
        foreach (AnimationTrack track in timeline.GetOutputTracks().OfType<AnimationTrack>())
        {
            AnimationClip animationClip = track.infiniteClip;

            if (animationClip == null)
            {
                continue;
            }

            EditorCurveBinding alphaBinding = AnimationUtility.GetCurveBindings(animationClip)
                .FirstOrDefault(binding => binding.propertyName == "m_Alpha");

            if (string.IsNullOrEmpty(alphaBinding.propertyName))
            {
                continue;
            }

            AnimationCurve alphaCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.8f, 0f),
                new Keyframe(4.5f, 0f),
                new Keyframe(4.9f, 1f),
                new Keyframe(5.1f, 1f),
                new Keyframe(5.5f, 0f),
                new Keyframe(8.5f, 0f),
                new Keyframe(8.9f, 1f),
                new Keyframe(9.1f, 1f),
                new Keyframe(9.5f, 0f),
                new Keyframe(12.5f, 0f),
                new Keyframe(12.9f, 1f),
                new Keyframe(13.1f, 1f),
                new Keyframe(13.5f, 0f),
                new Keyframe(17f, 0f),
                new Keyframe(17.4f, 1f),
                new Keyframe(17.6f, 1f),
                new Keyframe(18f, 0f),
                new Keyframe(27.5f, 0f),
                new Keyframe(27.9f, 1f),
                new Keyframe(28.1f, 1f),
                new Keyframe(28.5f, 0f),
                new Keyframe(CutsceneEnd, 0f)
            );

            SetClampedTangents(alphaCurve);
            AnimationUtility.SetEditorCurve(animationClip, alphaBinding, alphaCurve);
            animationClip.frameRate = 60f;
            EditorUtility.SetDirty(animationClip);
            EditorUtility.SetDirty(track);
            return;
        }

        Debug.LogWarning("FadeOverlay animation track was not found. Camera timing was updated without fade keys.");
    }

    private static void ConfigureExistingDollyTrack(
        TimelineAsset timeline,
        PlayableDirector director,
        Animator animator,
        float startTime,
        float endTime)
    {
        AnimationTrack track = timeline.GetOutputTracks()
            .OfType<AnimationTrack>()
            .FirstOrDefault(candidate => director.GetGenericBinding(candidate) == animator);

        if (track == null || track.infiniteClip == null)
        {
            Debug.LogWarning("The Establish camera animation track could not be found, so its existing movement was preserved.");
            return;
        }

        EditorCurveBinding positionBinding = AnimationUtility.GetCurveBindings(track.infiniteClip)
            .FirstOrDefault(binding => binding.propertyName == "m_SplineSettings.Position");

        if (string.IsNullOrEmpty(positionBinding.propertyName))
        {
            Debug.LogWarning("The Establish camera track has no spline position curve, so its movement was preserved.");
            return;
        }

        AnimationCurve curve = new AnimationCurve(
            new Keyframe(startTime, 0f),
            new Keyframe(endTime, 1f)
        );

        SetClampedTangents(curve);
        AnimationUtility.SetEditorCurve(track.infiniteClip, positionBinding, curve);
        EditorUtility.SetDirty(track.infiniteClip);
        EditorUtility.SetDirty(track);
    }

    private static AnimationTrack CreateDollyTrack(
        TimelineAsset timeline,
        string trackName,
        string clipName,
        float startTime,
        float endTime)
    {
        AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, trackName);
        track.CreateInfiniteClip(clipName);

        AnimationCurve curve = new AnimationCurve(
            new Keyframe(startTime, 0f),
            new Keyframe(endTime, 1f)
        );

        SetClampedTangents(curve);

        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
            string.Empty,
            typeof(Unity.Cinemachine.CinemachineSplineDolly),
            "m_SplineSettings.Position"
        );

        AnimationUtility.SetEditorCurve(track.infiniteClip, binding, curve);
        track.infiniteClip.frameRate = 60f;
        EditorUtility.SetDirty(track.infiniteClip);
        EditorUtility.SetDirty(track);
        return track;
    }

    private static void ConfigurePlayerRevealMovement(
        AnimationTrack track)
    {
        if (track == null || track.infiniteClip == null)
        {
            return;
        }

        EditorCurveBinding positionBinding = EditorCurveBinding.FloatCurve(
            string.Empty,
            typeof(CinemachineSplineDolly),
            "m_SplineSettings.Position"
        );

        AnimationCurve positionCurve = new AnimationCurve(
            new Keyframe(ThreatEnd, 0f),
            new Keyframe(20.5f, 0.28f),
            new Keyframe(25.5f, 0.86f),
            new Keyframe(PlayerEnd, 1f)
        );

        SetClampedTangents(positionCurve);
        AnimationUtility.SetEditorCurve(track.infiniteClip, positionBinding, positionCurve);
        EditorUtility.SetDirty(track.infiniteClip);
        EditorUtility.SetDirty(track);
    }

    private static void SetClampedTangents(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
    }

    private static void RemoveTrackIfPresent(TimelineAsset timeline, string trackName)
    {
        TrackAsset existingTrack = timeline.GetOutputTracks().FirstOrDefault(track => track.name == trackName);

        if (existingTrack != null)
        {
            timeline.DeleteTrack(existingTrack);
        }
    }

    private static Animator GetOrAddAnimator(GameObject gameObject)
    {
        Animator animator = gameObject.GetComponent<Animator>();

        if (animator == null)
        {
            animator = Undo.AddComponent<Animator>(gameObject);
        }

        return animator;
    }

    private static void ShowError(bool showDialog, string message)
    {
        Debug.LogError(message);

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Intro Dolly Setup Failed", message, "OK");
        }
    }
}
