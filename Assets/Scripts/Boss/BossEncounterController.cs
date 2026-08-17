using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossEncounterController : MonoBehaviour
{
    private const string DefeatBossObjectiveId = "defeat_boss";
    private const string ClearFinalEncounterObjectiveId = "clear_final_encounter";

    [Header("References")]
    [SerializeField] private EnemyEncounterZone finalEncounter;
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private ExtractionZone extractionZone;
    [SerializeField] private BossHealthBarUI bossHealthBar;

    [Header("Drop Sequence")]
    [SerializeField, Min(0.1f)] private float dropHeight = 9f;
    [SerializeField, Min(0.1f)] private float dropDuration = 0.8f;
    [SerializeField, Min(0f)] private float landingSettleDuration = 1.25f;
    [SerializeField, Range(0f, 0.95f)] private float landingStartNormalizedTime = 0.45f;

    [Header("Testing")]
    [SerializeField] private bool bossOnlyTestMode;

    [Header("Objective Text")]
    [SerializeField] private string defeatBossTitle = "Defeat the boss";
    [SerializeField] private string defeatBossDescription = "Destroy the mech blocking the extraction route.";
    [SerializeField] private string clearRemainingEnemiesTitle = "Clear the remaining enemies";
    [SerializeField] private string clearRemainingEnemiesDescription = "Eliminate the remaining enemies in the final area.";

    private Transform bossTransform;
    private Vector3 landingPosition;
    private Quaternion landingRotation;
    private Coroutine dropRoutine;
    private bool bossSpawned;
    private bool bossDefeated;

    private void Awake()
    {
        ResolveSceneReferences();

        if (!HasRequiredReferences())
        {
            enabled = false;
            return;
        }

        bossTransform = bossHealth.transform;
        landingPosition = bossTransform.position;
        landingRotation = bossTransform.rotation;
        SetupBossHealthBar();
        finalEncounter.SetEnemySpawningSuppressed(bossOnlyTestMode);
        bossHealth.gameObject.SetActive(false);
    }

    private void ResolveSceneReferences()
    {
        if (bossHealth == null || !bossHealth.gameObject.scene.IsValid())
        {
            bossHealth = GetComponentInChildren<BossHealth>(true);
        }
    }

    private void OnEnable()
    {
        if (!enabled)
        {
            return;
        }

        finalEncounter.EncounterActivated += HandleFinalEncounterActivated;
        finalEncounter.EncounterCompleted += HandleFinalEncounterCompleted;
        bossHealth.Died += HandleBossDied;

        if (finalEncounter.HasActivated)
        {
            StartBossDrop();
        }
    }

    private void OnDisable()
    {
        if (finalEncounter != null)
        {
            finalEncounter.EncounterActivated -= HandleFinalEncounterActivated;
            finalEncounter.EncounterCompleted -= HandleFinalEncounterCompleted;
        }

        if (bossHealth != null)
        {
            bossHealth.Died -= HandleBossDied;
        }

        if (dropRoutine != null)
        {
            StopCoroutine(dropRoutine);
            dropRoutine = null;
        }
    }

    [ContextMenu("Debug/Trigger Boss Drop")]
    private void DebugTriggerBossDrop()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        StartBossDrop();
#endif
    }

    private void HandleFinalEncounterActivated(EnemyEncounterZone activatedEncounter)
    {
        if (activatedEncounter == finalEncounter)
        {
            StartBossDrop();
        }
    }

    private void HandleFinalEncounterCompleted(EnemyEncounterZone completedEncounter)
    {
        if (completedEncounter != finalEncounter)
        {
            return;
        }

        if (!bossSpawned && dropRoutine == null)
        {
            StartBossDrop();
        }

        TryUnlockExtraction();
    }

    private void HandleBossDied(BossHealth defeatedBoss)
    {
        if (defeatedBoss != bossHealth || bossDefeated)
        {
            return;
        }

        bossDefeated = true;
        TryUnlockExtraction();
    }

    private void StartBossDrop()
    {
        if (!enabled || bossSpawned || dropRoutine != null || bossDefeated)
        {
            return;
        }

        dropRoutine = StartCoroutine(DropBossRoutine());
    }

    private IEnumerator DropBossRoutine()
    {
        bossTransform.position = landingPosition + Vector3.up * dropHeight;
        bossTransform.rotation = landingRotation;
        bossHealth.gameObject.SetActive(true);
        yield return null;
        bossHealth.SetDormantState();

        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / dropDuration);
            float fallProgress = normalizedTime * normalizedTime;
            bossTransform.position = Vector3.LerpUnclamped(landingPosition + Vector3.up * dropHeight, landingPosition, fallProgress);
            yield return null;
        }

        bossTransform.position = landingPosition;
        bossHealth.PlayLandingAnimation(landingStartNormalizedTime);

        if (landingSettleDuration > 0f)
        {
            yield return new WaitForSeconds(landingSettleDuration);
        }

        bossHealth.ActivateBoss();
        bossSpawned = true;
        dropRoutine = null;
        objectiveManager.SetObjective(DefeatBossObjectiveId, defeatBossTitle, defeatBossDescription, bossTransform);
        Debug.Log("Boss drop complete. Boss activated.", this);
    }

    private void TryUnlockExtraction()
    {
        if (!bossDefeated)
        {
            return;
        }

        if (!finalEncounter.IsCompleted)
        {
            objectiveManager.SetObjective(ClearFinalEncounterObjectiveId, clearRemainingEnemiesTitle, clearRemainingEnemiesDescription, finalEncounter.GuidanceTarget);
            return;
        }

        extractionZone.UnlockExtraction();
        Debug.Log("Boss and final encounter defeated. Extraction unlocked.", this);
    }

    private void SetupBossHealthBar()
    {
        if (bossHealthBar == null)
        {
            Debug.LogError($"{name}: Missing scene Boss Health Bar reference.", this);
            return;
        }

        bossHealthBar.Initialize(bossHealth);
    }

    private bool HasRequiredReferences()
    {
        if (finalEncounter == null || bossHealth == null || objectiveManager == null || extractionZone == null)
        {
            Debug.LogError($"{name}: Missing required boss encounter reference.", this);
            return false;
        }

        return true;
    }
}
