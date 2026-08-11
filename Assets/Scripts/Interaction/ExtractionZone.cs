using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class ExtractionZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyEncounterZone finalEncounter;
    [SerializeField] private GameObject zoneVisual;

    private BoxCollider zoneTrigger;
    private bool isUnlocked;
    private bool isCompleted;

    public bool IsUnlocked => isUnlocked;
    public bool IsCompleted => isCompleted;

    public event Action ExtractionUnlocked;
    public event Action ExtractionCompleted;

    private void Awake()
    {
        zoneTrigger = GetComponent<BoxCollider>();

        if (!zoneTrigger.isTrigger)
        {
            Debug.LogError($"{name}: BoxCollider must have Is Trigger enabled.", this);
        }

        ApplyLockedState();
    }

    private void OnEnable()
    {
        if (finalEncounter == null)
        {
            Debug.LogError($"{name}: Missing Final Encounter reference.", this);
            return;
        }

        finalEncounter.EncounterCompleted += HandleFinalEncounterCompleted;

        if (finalEncounter.IsCompleted)
        {
            Unlock();
        }
    }

    private void OnDisable()
    {
        if (finalEncounter != null)
        {
            finalEncounter.EncounterCompleted -= HandleFinalEncounterCompleted;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isUnlocked || isCompleted || !IsPlayer(other))
        {
            return;
        }

        isCompleted = true;

        Debug.Log($"{name}: Extraction completed.", this);
        ExtractionCompleted?.Invoke();
    }

    private void HandleFinalEncounterCompleted(EnemyEncounterZone completedEncounter)
    {
        if (completedEncounter != finalEncounter)
        {
            return;
        }

        Unlock();
    }

    private void Unlock()
    {
        if (isUnlocked)
        {
            return;
        }

        isUnlocked = true;
        zoneTrigger.enabled = true;

        if (zoneVisual != null)
        {
            zoneVisual.SetActive(true);
        }

        Debug.Log($"{name}: Extraction unlocked.", this);
        ExtractionUnlocked?.Invoke();
    }

    private void ApplyLockedState()
    {
        isUnlocked = false;
        isCompleted = false;

        if (zoneTrigger != null)
        {
            zoneTrigger.enabled = false;
        }

        if (zoneVisual != null)
        {
            zoneVisual.SetActive(false);
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
               other.transform.root.CompareTag("Player");
    }
}
