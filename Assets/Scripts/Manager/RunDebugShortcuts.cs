using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunDebugShortcuts : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundProgressManager roundProgressManager;
    [SerializeField] private EnemyEncounterZone finalEncounter;
    [SerializeField] private Transform finalStageTarget;
    [SerializeField] private Transform player;

    [Header("Shortcuts")]
    [SerializeField] private KeyCode completeRoundKey = KeyCode.F8;
    [SerializeField] private KeyCode teleportToFinalStageKey = KeyCode.F9;
    [SerializeField] private KeyCode completeFinalEncounterKey = KeyCode.F10;

    private void Update()
    {
        if (Input.GetKeyDown(completeRoundKey))
        {
            CompleteRoundEncounters();
        }

        if (Input.GetKeyDown(teleportToFinalStageKey))
        {
            TeleportToFinalStage();
        }

        if (Input.GetKeyDown(completeFinalEncounterKey))
        {
            CompleteFinalEncounter();
        }
    }

    private void CompleteRoundEncounters()
    {
        if (roundProgressManager == null)
        {
            Debug.LogError($"{name}: Missing RoundProgressManager.", this);
            return;
        }

        roundProgressManager.DebugCompleteAllEncounters();
        Debug.Log("[DEBUG] Completed all regular encounters.", this);
    }

    private void TeleportToFinalStage()
    {
        if (roundProgressManager == null || !roundProgressManager.IsRoundCompleted)
        {
            Debug.LogWarning("[DEBUG] Complete the regular encounters with F8 first.", this);
            return;
        }

        if (finalStageTarget == null)
        {
            Debug.LogError($"{name}: Missing Final Stage Target.", this);
            return;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }

        if (player == null)
        {
            Debug.LogError($"{name}: Cannot find an object tagged Player.", this);
            return;
        }

        CharacterController characterController = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = characterController != null && characterController.enabled;

        if (controllerWasEnabled)
        {
            characterController.enabled = false;
        }

        player.position = finalStageTarget.position + Vector3.up * 0.25f;

        if (controllerWasEnabled)
        {
            characterController.enabled = true;
        }

        Physics.SyncTransforms();
        Debug.Log("[DEBUG] Teleported the player to the Final Encounter.", this);
    }

    private void CompleteFinalEncounter()
    {
        if (finalEncounter == null)
        {
            Debug.LogError($"{name}: Missing Final Encounter.", this);
            return;
        }

        finalEncounter.DebugCompleteEncounter();
        Debug.Log("[DEBUG] Completed the Final Encounter.", this);
    }
}
