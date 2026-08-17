using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoundObjectiveBridge : MonoBehaviour
{
    private const string ClearEncountersId = "clear_encounters";
    private const string ReachFinalAreaId = "reach_final_area";
    private const string ReachExtractionId = "reach_extraction";
    private const string ExtractionCompletedId = "extraction_completed";

    [Header("References")]
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private RoundProgressManager roundProgressManager;
    [SerializeField] private ExtractionZone extractionZone;
    [SerializeField] private EncounterGuidanceSelector guidanceSelector;

    [Header("World Targets")]
    [SerializeField] private Transform finalAreaTarget;

    [Header("Objective Text")]
    [SerializeField] private string clearEncountersTitle = "Clear encounters";
    [SerializeField] private string clearEncountersDescription = "Complete the three encounters to unlock the final area.";
    [SerializeField] private string reachFinalAreaTitle = "Reach the final area";
    [SerializeField] private string reachFinalAreaDescription = "The final gate is open.";
    [SerializeField] private string reachExtractionTitle = "Reach the extraction point";
    [SerializeField] private string reachExtractionDescription = "The extraction point is now active.";
    [SerializeField] private string extractionCompletedTitle = "Extraction successful";
    [SerializeField] private string extractionCompletedDescription = "The mission is complete.";

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        roundProgressManager.ProgressChanged += HandleProgressChanged;
        roundProgressManager.AllEncountersCompleted += HandleAllEncountersCompleted;
        extractionZone.ExtractionUnlocked += HandleExtractionUnlocked;
        extractionZone.ExtractionCompleted += HandleExtractionCompleted;
        guidanceSelector.GuidanceEncounterChanged += HandleGuidanceEncounterChanged;
        RefreshObjective();
    }

    private void OnDisable()
    {
        if (roundProgressManager != null)
        {
            roundProgressManager.ProgressChanged -= HandleProgressChanged;
            roundProgressManager.AllEncountersCompleted -= HandleAllEncountersCompleted;
        }

        if (extractionZone != null)
        {
            extractionZone.ExtractionUnlocked -= HandleExtractionUnlocked;
            extractionZone.ExtractionCompleted -= HandleExtractionCompleted;
        }

        if (guidanceSelector != null)
        {
            guidanceSelector.GuidanceEncounterChanged -= HandleGuidanceEncounterChanged;
        }
    }

    private void HandleProgressChanged(int completedCount, int totalCount)
    {
        RefreshObjective();
    }

    private void HandleAllEncountersCompleted()
    {
        RefreshObjective();
    }

    private void HandleExtractionUnlocked()
    {
        RefreshObjective();
    }

    private void HandleExtractionCompleted()
    {
        RefreshObjective();
    }

    private void HandleGuidanceEncounterChanged(EnemyEncounterZone encounter)
    {
        RefreshObjective();
    }

    private void RefreshObjective()
    {
        if (extractionZone.IsCompleted)
        {
            SetExtractionCompletedObjective();
            return;
        }

        if (extractionZone.IsUnlocked)
        {
            SetObjectiveIfNeeded(ReachExtractionId, reachExtractionTitle, reachExtractionDescription, extractionZone.transform);
            return;
        }

        if (roundProgressManager.IsRoundCompleted)
        {
            SetObjectiveIfNeeded(ReachFinalAreaId, reachFinalAreaTitle, reachFinalAreaDescription, finalAreaTarget);
            return;
        }

        SetClearEncountersObjective();
    }

    private void SetClearEncountersObjective()
    {
        Transform currentTarget = guidanceSelector.CurrentTarget;

        if (!objectiveManager.IsCurrentObjective(ClearEncountersId))
        {
            objectiveManager.SetObjective(ClearEncountersId, clearEncountersTitle, clearEncountersDescription, currentTarget, roundProgressManager.TotalCount);
        }
        else
        {
            objectiveManager.UpdateWorldTarget(ClearEncountersId, currentTarget);
        }

        objectiveManager.UpdateProgress(ClearEncountersId, roundProgressManager.CompletedCount);
    }

    private void SetExtractionCompletedObjective()
    {
        SetObjectiveIfNeeded(ExtractionCompletedId, extractionCompletedTitle, extractionCompletedDescription, extractionZone.transform);
        objectiveManager.CompleteObjective(ExtractionCompletedId);
    }

    private void SetObjectiveIfNeeded(string objectiveId, string title, string description, Transform worldTarget)
    {
        if (objectiveManager.IsCurrentObjective(objectiveId))
        {
            return;
        }

        objectiveManager.SetObjective(objectiveId, title, description, worldTarget);
    }

    private bool HasRequiredReferences()
    {
        if (objectiveManager == null)
        {
            Debug.LogError($"{name}: Missing ObjectiveManager.", this);
            return false;
        }

        if (roundProgressManager == null)
        {
            Debug.LogError($"{name}: Missing RoundProgressManager.", this);
            return false;
        }

        if (extractionZone == null)
        {
            Debug.LogError($"{name}: Missing ExtractionZone.", this);
            return false;
        }

        if (guidanceSelector == null)
        {
            Debug.LogError($"{name}: Missing EncounterGuidanceSelector.", this);
            return false;
        }

        return true;
    }
}
