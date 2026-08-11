using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ObjectiveHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundProgressManager roundProgressManager;
    [SerializeField] private ExtractionZone extractionZone;
    [SerializeField] private TMP_Text objectiveText;

    [Header("Text")]
    [SerializeField] private string progressPrefix = "Clear encounters";
    [SerializeField] private string completedMessage = "Reach the final area";
    [SerializeField] private string extractionMessage = "Reach the extraction point";
    [SerializeField] private string extractionCompletedMessage = "Extraction successful";

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        roundProgressManager.ProgressChanged += HandleProgressChanged;
        extractionZone.ExtractionUnlocked += HandleExtractionUnlocked;
        extractionZone.ExtractionCompleted += HandleExtractionCompleted;
        RefreshCurrentState();
    }

    private void OnDisable()
    {
        if (roundProgressManager != null)
        {
            roundProgressManager.ProgressChanged -= HandleProgressChanged;
        }

        if (extractionZone != null)
        {
            extractionZone.ExtractionUnlocked -= HandleExtractionUnlocked;
            extractionZone.ExtractionCompleted -= HandleExtractionCompleted;
        }
    }

    private void HandleProgressChanged(int completedCount, int totalCount)
    {
        RefreshCurrentState();
    }

    private void HandleExtractionUnlocked()
    {
        objectiveText.text = extractionMessage;
    }

    private void HandleExtractionCompleted()
    {
        objectiveText.text = extractionCompletedMessage;
    }

    private void RefreshCurrentState()
    {
        if (extractionZone.IsCompleted)
        {
            objectiveText.text = extractionCompletedMessage;
            return;
        }

        if (extractionZone.IsUnlocked)
        {
            objectiveText.text = extractionMessage;
            return;
        }

        RefreshRoundProgress(
            roundProgressManager.CompletedCount,
            roundProgressManager.TotalCount
        );
    }

    private void RefreshRoundProgress(int completedCount, int totalCount)
    {
        if (objectiveText == null)
        {
            return;
        }

        if (totalCount > 0 && completedCount >= totalCount)
        {
            objectiveText.text = completedMessage;
            return;
        }

        objectiveText.text = $"{progressPrefix}: {completedCount}/{totalCount}";
    }

    private bool HasRequiredReferences()
    {
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

        if (objectiveText == null)
        {
            Debug.LogError($"{name}: Missing Objective Text.", this);
            return false;
        }

        return true;
    }
}
