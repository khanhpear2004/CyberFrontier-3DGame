using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ObjectiveHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private TMP_Text objectiveText;

    [Header("Formatting")]
    [SerializeField] private string progressSeparator = ": ";

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        objectiveManager.ObjectiveChanged += HandleObjectiveChanged;
        objectiveManager.ObjectiveCompleted += HandleObjectiveCompleted;
        objectiveManager.ObjectiveCleared += HandleObjectiveCleared;
        RefreshCurrentObjective();
    }

    private void OnDisable()
    {
        if (objectiveManager == null)
        {
            return;
        }

        objectiveManager.ObjectiveChanged -= HandleObjectiveChanged;
        objectiveManager.ObjectiveCompleted -= HandleObjectiveCompleted;
        objectiveManager.ObjectiveCleared -= HandleObjectiveCleared;
    }

    private void HandleObjectiveChanged(ObjectiveState objective)
    {
        DisplayObjective(objective);
    }

    private void HandleObjectiveCompleted(ObjectiveState objective)
    {
        DisplayObjective(objective);
    }

    private void HandleObjectiveCleared()
    {
        objectiveText.text = string.Empty;
    }

    private void RefreshCurrentObjective()
    {
        if (!objectiveManager.HasActiveObjective)
        {
            objectiveText.text = string.Empty;
            return;
        }

        DisplayObjective(objectiveManager.CurrentObjective);
    }

    private void DisplayObjective(ObjectiveState objective)
    {
        if (objective == null)
        {
            objectiveText.text = string.Empty;
            return;
        }

        string progressText = objective.HasProgress ? $"{progressSeparator}{objective.CurrentProgress}/{objective.RequiredProgress}" : string.Empty;
        objectiveText.text = $"{objective.Title}{progressText}";
    }

    private bool HasRequiredReferences()
    {
        if (objectiveManager == null)
        {
            Debug.LogError($"{name}: Missing ObjectiveManager.", this);
            return false;
        }

        if (objectiveText == null)
        {
            Debug.LogError($"{name}: Missing Objective Text.", this);
            return false;
        }

        return true;
    }
}
