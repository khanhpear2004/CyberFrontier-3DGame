using UnityEngine;

[DisallowMultipleComponent]
public class EncounterChestUnlocker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyEncounterZone encounterZone;
    [SerializeField] private ChestInteractable targetChest;

    private void Awake()
    {
        if (encounterZone == null)
        {
            encounterZone = GetComponent<EnemyEncounterZone>();
        }
    }

    private void OnEnable()
    {
        if (encounterZone == null)
        {
            Debug.LogError($"{name}: Thiếu EnemyEncounterZone.", this);
            return;
        }

        if (targetChest == null)
        {
            Debug.LogError($"{name}: Thiếu ChestInteractable cần mở khóa.", this);
        }

        encounterZone.EncounterCompleted += HandleEncounterCompleted;

        if (encounterZone.IsCompleted)
        {
            UnlockTargetChest();
        }
    }

    private void OnDisable()
    {
        if (encounterZone != null)
        {
            encounterZone.EncounterCompleted -= HandleEncounterCompleted;
        }
    }

    private void HandleEncounterCompleted(EnemyEncounterZone completedEncounter)
    {
        if (completedEncounter != encounterZone)
        {
            return;
        }

        UnlockTargetChest();
    }

    private void UnlockTargetChest()
    {
        if (targetChest == null)
        {
            return;
        }

        targetChest.Unlock();
    }
}
