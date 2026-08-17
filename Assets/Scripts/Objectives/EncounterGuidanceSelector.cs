using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EncounterGuidanceSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundProgressManager roundProgressManager;
    [SerializeField] private Transform player;
    [SerializeField] private EnemyEncounterZone tutorialEncounter;

    [Header("Debug")]
    [SerializeField] private bool logTargetChanges = true;

    private readonly HashSet<EnemyEncounterZone> subscribedEncounters = new();

    public EnemyEncounterZone CurrentEncounter { get; private set; }
    public Transform CurrentTarget => CurrentEncounter != null ? CurrentEncounter.GuidanceTarget : null;

    public event Action<EnemyEncounterZone> GuidanceEncounterChanged;

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        SubscribeToEncounters();
        SelectInitialEncounter();
    }

    private void OnDisable()
    {
        UnsubscribeFromEncounters();
    }

    public void RefreshSelection()
    {
        if (!HasRequiredReferences())
        {
            SetCurrentEncounter(null);
            return;
        }

        if (CurrentEncounter != null && !CurrentEncounter.IsCompleted)
        {
            return;
        }

        SelectNearestIncompleteEncounter();
    }

    private void SubscribeToEncounters()
    {
        UnsubscribeFromEncounters();

        foreach (EnemyEncounterZone encounter in roundProgressManager.Encounters)
        {
            if (encounter == null || !subscribedEncounters.Add(encounter))
            {
                continue;
            }

            encounter.EncounterActivated += HandleEncounterActivated;
            encounter.EncounterCompleted += HandleEncounterCompleted;
        }
    }

    private void UnsubscribeFromEncounters()
    {
        foreach (EnemyEncounterZone encounter in subscribedEncounters)
        {
            if (encounter == null)
            {
                continue;
            }

            encounter.EncounterActivated -= HandleEncounterActivated;
            encounter.EncounterCompleted -= HandleEncounterCompleted;
        }

        subscribedEncounters.Clear();
    }

    private void SelectInitialEncounter()
    {
        EnemyEncounterZone activeEncounter = FindNearestIncompleteEncounter(true);

        if (activeEncounter != null)
        {
            SetCurrentEncounter(activeEncounter);
            return;
        }

        if (!tutorialEncounter.IsCompleted)
        {
            SetCurrentEncounter(tutorialEncounter);
            return;
        }

        SelectNearestIncompleteEncounter();
    }

    private void SelectNearestIncompleteEncounter()
    {
        SetCurrentEncounter(FindNearestIncompleteEncounter(false));
    }

    private EnemyEncounterZone FindNearestIncompleteEncounter(bool requireActivated)
    {
        EnemyEncounterZone nearestEncounter = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (EnemyEncounterZone encounter in roundProgressManager.Encounters)
        {
            if (encounter == null || encounter.IsCompleted || requireActivated && !encounter.HasActivated)
            {
                continue;
            }

            Vector2 encounterPosition = new Vector2(encounter.GuidanceTarget.position.x, encounter.GuidanceTarget.position.z);
            Vector2 playerPosition = new Vector2(player.position.x, player.position.z);
            float distance = (encounterPosition - playerPosition).sqrMagnitude;

            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestEncounter = encounter;
            nearestDistance = distance;
        }

        return nearestEncounter;
    }

    private void HandleEncounterActivated(EnemyEncounterZone encounter)
    {
        if (encounter == null || encounter.IsCompleted || !subscribedEncounters.Contains(encounter))
        {
            return;
        }

        SetCurrentEncounter(encounter);
    }

    private void HandleEncounterCompleted(EnemyEncounterZone encounter)
    {
        if (encounter != CurrentEncounter && CurrentEncounter != null && !CurrentEncounter.IsCompleted)
        {
            return;
        }

        SelectNearestIncompleteEncounter();
    }

    private void SetCurrentEncounter(EnemyEncounterZone encounter)
    {
        if (CurrentEncounter == encounter)
        {
            return;
        }

        CurrentEncounter = encounter;

        if (logTargetChanges)
        {
            string targetName = CurrentEncounter != null ? CurrentEncounter.name : "None";
            Debug.Log($"Guidance encounter changed: {targetName}.", this);
        }

        GuidanceEncounterChanged?.Invoke(CurrentEncounter);
    }

    private bool HasRequiredReferences()
    {
        if (roundProgressManager == null)
        {
            Debug.LogError($"{name}: Missing RoundProgressManager.", this);
            return false;
        }

        if (player == null)
        {
            Debug.LogError($"{name}: Missing Player Transform.", this);
            return false;
        }

        if (tutorialEncounter == null)
        {
            Debug.LogError($"{name}: Missing Tutorial Encounter.", this);
            return false;
        }

        foreach (EnemyEncounterZone encounter in roundProgressManager.Encounters)
        {
            if (encounter == tutorialEncounter)
            {
                return true;
            }
        }

        Debug.LogError($"{name}: Tutorial Encounter is not registered in RoundProgressManager.", this);
        return false;
    }
}
