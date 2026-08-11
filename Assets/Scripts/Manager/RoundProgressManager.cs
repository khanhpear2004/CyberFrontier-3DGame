using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoundProgressManager : MonoBehaviour
{
    [Header("Encounters")]
    [SerializeField] private List<EnemyEncounterZone> encounters = new();

    private readonly HashSet<EnemyEncounterZone> completedEncounters = new();
    private readonly HashSet<EnemyEncounterZone> subscribedEncounters = new();
    private bool completionEventRaised;

    public int CompletedCount => completedEncounters.Count;
    public int TotalCount => encounters.Count;
    public bool IsRoundCompleted =>
        TotalCount > 0 && CompletedCount == TotalCount;

    public event Action<int, int> ProgressChanged;
    public event Action AllEncountersCompleted;

    [ContextMenu("Debug/Complete All Encounters")]
    public void DebugCompleteAllEncounters()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        foreach (EnemyEncounterZone encounter in encounters)
        {
            if (encounter != null)
            {
                encounter.DebugCompleteEncounter();
            }
        }
#endif
    }

    private void OnEnable()
    {
        RebuildProgress();
        PublishProgress();
    }

    private void OnDisable()
    {
        foreach (EnemyEncounterZone encounter in subscribedEncounters)
        {
            if (encounter != null)
            {
                encounter.EncounterCompleted -= HandleEncounterCompleted;
            }
        }

        subscribedEncounters.Clear();
    }

    private void RebuildProgress()
    {
        completedEncounters.Clear();
        subscribedEncounters.Clear();

        foreach (EnemyEncounterZone encounter in encounters)
        {
            if (encounter == null)
            {
                Debug.LogError($"{name}: Danh sách encounter có phần tử None.", this);
                continue;
            }

            if (!subscribedEncounters.Add(encounter))
            {
                Debug.LogError($"{name}: Encounter {encounter.name} bị gắn trùng.", this);
                continue;
            }

            encounter.EncounterCompleted += HandleEncounterCompleted;

            if (encounter.IsCompleted)
            {
                completedEncounters.Add(encounter);
            }
        }
    }

    private void HandleEncounterCompleted(EnemyEncounterZone encounter)
    {
        if (encounter == null || !subscribedEncounters.Contains(encounter))
        {
            return;
        }

        if (!completedEncounters.Add(encounter))
        {
            return;
        }

        PublishProgress();
    }

    private void PublishProgress()
    {
        Debug.Log($"Round Progress: {CompletedCount}/{TotalCount}", this);
        ProgressChanged?.Invoke(CompletedCount, TotalCount);

        if (!IsRoundCompleted || completionEventRaised)
        {
            return;
        }

        completionEventRaised = true;
        Debug.Log("All encounters completed.", this);
        AllEncountersCompleted?.Invoke();
    }
}
