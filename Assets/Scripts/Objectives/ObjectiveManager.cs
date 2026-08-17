using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ObjectiveManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool logObjectiveChanges = true;

    private bool currentObjectiveCompleted;

    public ObjectiveState CurrentObjective { get; private set; }
    public bool HasActiveObjective => CurrentObjective != null;
    public bool IsCurrentObjectiveCompleted => HasActiveObjective && currentObjectiveCompleted;

    public event Action<ObjectiveState> ObjectiveChanged;
    public event Action<ObjectiveState> ObjectiveCompleted;
    public event Action ObjectiveCleared;

    public bool SetObjective(string objectiveId, string title, string description, Transform worldTarget = null, int requiredProgress = 0)
    {
        if (string.IsNullOrWhiteSpace(objectiveId))
        {
            Debug.LogError($"{name}: Objective ID không được để trống.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            Debug.LogError($"{name}: Objective title không được để trống.", this);
            return false;
        }

        CurrentObjective = new ObjectiveState(objectiveId, title, description, 0, requiredProgress, worldTarget);
        currentObjectiveCompleted = false;

        PublishObjectiveChanged();
        return true;
    }

    public bool UpdateProgress(string objectiveId, int newProgress)
    {
        if (!IsCurrentObjective(objectiveId))
        {
            return false;
        }

        if (currentObjectiveCompleted || !CurrentObjective.HasProgress)
        {
            return false;
        }

        int clampedProgress = Mathf.Clamp(newProgress, 0, CurrentObjective.RequiredProgress);

        if (clampedProgress == CurrentObjective.CurrentProgress)
        {
            return false;
        }

        CurrentObjective = CurrentObjective.WithProgress(clampedProgress);
        PublishObjectiveChanged();

        return true;
    }

    public bool UpdateWorldTarget(string objectiveId, Transform newWorldTarget)
    {
        if (!IsCurrentObjective(objectiveId))
        {
            return false;
        }

        if (CurrentObjective.WorldTarget == newWorldTarget)
        {
            return false;
        }

        CurrentObjective = CurrentObjective.WithWorldTarget(newWorldTarget);
        PublishObjectiveChanged();

        return true;
    }

    public bool CompleteObjective(string objectiveId)
    {
        if (!IsCurrentObjective(objectiveId) || currentObjectiveCompleted)
        {
            return false;
        }

        if (CurrentObjective.HasProgress && CurrentObjective.CurrentProgress < CurrentObjective.RequiredProgress)
        {
            CurrentObjective = CurrentObjective.WithProgress(CurrentObjective.RequiredProgress);
            PublishObjectiveChanged();
        }

        currentObjectiveCompleted = true;

        if (logObjectiveChanges)
        {
            Debug.Log($"Objective completed: {CurrentObjective.Title}", this);
        }

        ObjectiveCompleted?.Invoke(CurrentObjective);
        return true;
    }

    public bool ClearObjective(string objectiveId)
    {
        if (!IsCurrentObjective(objectiveId))
        {
            return false;
        }

        CurrentObjective = null;
        currentObjectiveCompleted = false;

        if (logObjectiveChanges)
        {
            Debug.Log("Current objective cleared.", this);
        }

        ObjectiveCleared?.Invoke();
        return true;
    }

    public bool IsCurrentObjective(string objectiveId)
    {
        return HasActiveObjective && !string.IsNullOrWhiteSpace(objectiveId) && CurrentObjective.Id == objectiveId;
    }

    private void PublishObjectiveChanged()
    {
        if (logObjectiveChanges)
        {
            string progressText = CurrentObjective.HasProgress ? $" {CurrentObjective.CurrentProgress}/{CurrentObjective.RequiredProgress}" : string.Empty;
            string targetName = CurrentObjective.WorldTarget != null ? CurrentObjective.WorldTarget.name : "None";
            Debug.Log($"Objective: {CurrentObjective.Title}{progressText} | Target: {targetName}", this);
        }

        ObjectiveChanged?.Invoke(CurrentObjective);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Set Sample Objective")]
    private void DebugSetSampleObjective()
    {
        SetObjective("debug_solar_relay", "Reach the Solar Relay", "Follow the orange energy signal.", transform);
    }

    [ContextMenu("Debug/Set Sample Defend Objective")]
    private void DebugSetSampleDefendObjective()
    {
        SetObjective("debug_defend_solar", "Defend the Solar Relay", "Protect the relay until charging completes.", transform, 100);
    }

    [ContextMenu("Debug/Add 25 Progress")]
    private void DebugAddProgress()
    {
        if (!HasActiveObjective || !CurrentObjective.HasProgress)
        {
            return;
        }

        UpdateProgress(CurrentObjective.Id, CurrentObjective.CurrentProgress + 25);
    }

    [ContextMenu("Debug/Complete Current Objective")]
    private void DebugCompleteCurrentObjective()
    {
        if (!HasActiveObjective)
        {
            return;
        }

        CompleteObjective(CurrentObjective.Id);
    }
#endif
}