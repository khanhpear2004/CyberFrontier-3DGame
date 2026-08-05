using System.Collections.Generic;
using UnityEngine;

public sealed class PlanetBuffLoadoutPageUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private RunPlanetBuffManager runPlanetBuffManager;
    [SerializeField] private PlanetBuffLoadoutManager buffLoadoutManager;

    [Header("Slot Container")]
    [SerializeField] private Transform buffSlotContainer;

    [Header("Detail")]
    [SerializeField] private PlanetBuffDetailController buffDetailController;

    private PlanetBuffLoadoutSlotView[] buffSlots;

    private void Awake()
    {
        CollectBuffSlots();
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (buffLoadoutManager != null)
        {
            buffLoadoutManager.LoadoutChanged += HandleLoadoutChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (buffLoadoutManager != null)
        {
            buffLoadoutManager.LoadoutChanged -= HandleLoadoutChanged;
        }
    }

    public void Refresh()
    {
        ClearAllSlots();

        if (!HasRequiredReferences())
        {
            return;
        }

        IReadOnlyList<string> equippedBuffIds = buffLoadoutManager.EquippedBuffIds;
        int displayedBuffCount = Mathf.Min(equippedBuffIds.Count, buffSlots.Length);

        for (int i = 0; i < displayedBuffCount; i++)
        {
            string buffId = equippedBuffIds[i];

            if (!runPlanetBuffManager.TryGetOwnedBuff(buffId, out PlanetBuffData buffData) || buffData == null)
            {
                continue;
            }

            buffSlots[i].Bind(buffData, HandleSlotClicked);
        }
    }

    private void CollectBuffSlots()
    {
        if (buffSlotContainer == null)
        {
            buffSlots = new PlanetBuffLoadoutSlotView[0];
            return;
        }

        buffSlots = buffSlotContainer.GetComponentsInChildren<PlanetBuffLoadoutSlotView>(true);
    }

    private void ClearAllSlots()
    {
        if (buffSlots == null)
        {
            return;
        }

        foreach (PlanetBuffLoadoutSlotView buffSlot in buffSlots)
        {
            if (buffSlot != null)
            {
                buffSlot.SetEmpty();
            }
        }
    }

    private void HandleSlotClicked(PlanetBuffData buffData)
    {
        if (buffData == null || buffDetailController == null)
        {
            return;
        }

        buffDetailController.ShowBuff(buffData);
    }

    private void HandleLoadoutChanged()
    {
        Refresh();
    }

    private bool HasRequiredReferences()
    {
        return runPlanetBuffManager != null &&
               buffLoadoutManager != null &&
               buffSlots != null &&
               buffSlots.Length > 0;
    }

    private void ValidateReferences()
    {
        if (runPlanetBuffManager == null)
        {
            Debug.LogError($"{name}: RunPlanetBuffManager has not been assigned.", this);
        }

        if (buffLoadoutManager == null)
        {
            Debug.LogError($"{name}: PlanetBuffLoadoutManager has not been assigned.", this);
        }

        if (buffSlotContainer == null)
        {
            Debug.LogError($"{name}: BuffSlotContainer has not been assigned.", this);
            return;
        }

        if (buffSlots == null || buffSlots.Length == 0)
        {
            Debug.LogError($"{name}: No PlanetBuffLoadoutSlotView was found.", this);
            return;
        }

        if (buffLoadoutManager != null && buffSlots.Length != buffLoadoutManager.MaxEquippedBuffs)
        {
            Debug.LogWarning(
                $"{name}: Found {buffSlots.Length} buff slots, but " +
                $"PlanetBuffLoadoutManager allows {buffLoadoutManager.MaxEquippedBuffs}.",
                this
            );
        }
    }
}
