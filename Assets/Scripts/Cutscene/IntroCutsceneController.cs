using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class IntroCutsceneController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private bool playOnSceneStart = true;

    [Header("Cutscene UI")]
    [SerializeField] private GameObject introCutsceneUi;

    [Header("Player Input")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAimShooter playerAimShooter;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private InventoryMenuUI inventoryMenu;

    [Header("Camera Input")]
    [SerializeField] private CinemachineInputAxisController[] cameraInputsToDisable;

    [Header("Gameplay Camera Handoff")]
    [SerializeField] private CinemachineOrbitalFollow gameplayOrbit;
    [SerializeField] private bool alignCameraBehindPlayer = true;

    [Header("Encounters")]
    [SerializeField] private EnemyEncounterZone[] encountersToDisable;

    [Header("Gameplay HUD")]
    [SerializeField] private GameObject[] hudObjectsToHide;

    [Header("Skip")]
    [SerializeField] private KeyCode skipKey = KeyCode.Space;

    private Behaviour[] controlledBehaviours;
    private bool[] previousBehaviourStates;
    private bool[] previousHudStates;

    private bool isPlaying;
    private bool hasFinished;

    public bool IsPlaying => isPlaying;

    private void OnEnable()
    {
        if (director != null)
        {
            director.stopped += HandleDirectorStopped;
        }
    }

    private void Start()
    {
        if (playOnSceneStart)
        {
            BeginIntro();
        }
    }

    private void Update()
    {
        if (isPlaying && Input.GetKeyDown(skipKey))
        {
            SkipIntro();
        }
    }

    private void OnDisable()
    {
        if (director != null)
        {
            director.stopped -= HandleDirectorStopped;
        }
    }

    public void BeginIntro()
    {
        if (isPlaying || director == null)
        {
            return;
        }

        BuildControlledBehaviours();
        CacheInitialStates();

        isPlaying = true;
        hasFinished = false;

        if (inventoryMenu != null)
        {
            inventoryMenu.Close();
        }

        SetGameplayEnabled(false);
        AlignGameplayCameraBehindPlayer();

        if (introCutsceneUi != null)
        {
            introCutsceneUi.SetActive(true);
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        director.RebuildGraph();
        director.time = 0d;
        director.Evaluate();
        director.Play();
    }

    public void SkipIntro()
    {
        if (!isPlaying || director == null)
        {
            return;
        }

        director.time = director.duration;
        director.Evaluate();
        director.Stop();
    }

    private void HandleDirectorStopped(PlayableDirector stoppedDirector)
    {
        if (stoppedDirector == director)
        {
            FinishIntro();
        }
    }

    private void FinishIntro()
    {
        if (hasFinished)
        {
            return;
        }

        hasFinished = true;
        isPlaying = false;

        RestoreInitialStates();
        AlignGameplayCameraBehindPlayer();

        if (introCutsceneUi != null)
        {
            introCutsceneUi.SetActive(false);
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void AlignGameplayCameraBehindPlayer()
    {
        if (!alignCameraBehindPlayer || gameplayOrbit == null || playerController == null)
        {
            return;
        }

        float playerYaw = Mathf.DeltaAngle(0f, playerController.transform.eulerAngles.y);
        gameplayOrbit.HorizontalAxis.Value = playerYaw;
    }

    private void BuildControlledBehaviours()
    {
        List<Behaviour> behaviours = new List<Behaviour>();

        AddBehaviour(behaviours, playerController);
        AddBehaviour(behaviours, playerAimShooter);
        AddBehaviour(behaviours, playerInteractor);
        AddBehaviour(behaviours, inventoryMenu);

        if (cameraInputsToDisable != null)
        {
            foreach (CinemachineInputAxisController cameraInput in cameraInputsToDisable)
            {
                AddBehaviour(behaviours, cameraInput);
            }
        }

        if (encountersToDisable != null)
        {
            foreach (EnemyEncounterZone encounter in encountersToDisable)
            {
                AddBehaviour(behaviours, encounter);
            }
        }

        controlledBehaviours = behaviours.ToArray();
    }

    private static void AddBehaviour(List<Behaviour> behaviours, Behaviour behaviour)
    {
        if (behaviour != null && !behaviours.Contains(behaviour))
        {
            behaviours.Add(behaviour);
        }
    }

    private void CacheInitialStates()
    {
        int behaviourCount = controlledBehaviours != null ? controlledBehaviours.Length : 0;
        previousBehaviourStates = new bool[behaviourCount];

        for (int i = 0; i < behaviourCount; i++)
        {
            Behaviour behaviour = controlledBehaviours[i];
            previousBehaviourStates[i] = behaviour != null && behaviour.enabled;
        }

        int hudCount = hudObjectsToHide != null ? hudObjectsToHide.Length : 0;
        previousHudStates = new bool[hudCount];

        for (int i = 0; i < hudCount; i++)
        {
            GameObject hudObject = hudObjectsToHide[i];
            previousHudStates[i] = hudObject != null && hudObject.activeSelf;
        }
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (controlledBehaviours != null)
        {
            foreach (Behaviour behaviour in controlledBehaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = enabled;
                }
            }
        }

        if (hudObjectsToHide == null)
        {
            return;
        }

        foreach (GameObject hudObject in hudObjectsToHide)
        {
            if (hudObject != null)
            {
                hudObject.SetActive(enabled);
            }
        }
    }

    private void RestoreInitialStates()
    {
        if (controlledBehaviours != null && previousBehaviourStates != null)
        {
            int behaviourCount = Mathf.Min(
                controlledBehaviours.Length,
                previousBehaviourStates.Length
            );

            for (int i = 0; i < behaviourCount; i++)
            {
                Behaviour behaviour = controlledBehaviours[i];

                if (behaviour != null)
                {
                    behaviour.enabled = previousBehaviourStates[i];
                }
            }
        }

        if (hudObjectsToHide == null || previousHudStates == null)
        {
            return;
        }

        int hudCount = Mathf.Min(hudObjectsToHide.Length, previousHudStates.Length);

        for (int i = 0; i < hudCount; i++)
        {
            GameObject hudObject = hudObjectsToHide[i];

            if (hudObject != null)
            {
                hudObject.SetActive(previousHudStates[i]);
            }
        }
    }
}
