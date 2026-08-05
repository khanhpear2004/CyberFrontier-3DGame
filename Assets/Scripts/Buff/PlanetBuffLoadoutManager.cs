using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlanetBuffLoadoutManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RunPlanetBuffManager runPlanetBuffManager;
    [SerializeField] private RuntimeStats runtimeStats;

    [Header("Loadout")]
    [Min(1)]
    [SerializeField] private int maxEquippedBuffs = 6;
    private readonly List<string> equippedBuffIds = new List<string>();
    public event Action LoadoutChanged;

    public int MaxEquippedBuffs => maxEquippedBuffs;
    public int EquippedCount => equippedBuffIds.Count;
    public IReadOnlyList<string> EquippedBuffIds => equippedBuffIds;

    private void Awake()
    {
        if (runPlanetBuffManager == null)
        {
            runPlanetBuffManager = GetComponent<RunPlanetBuffManager>();
        }

        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<RuntimeStats>();
        }

        if (runPlanetBuffManager == null)
        {
            Debug.LogError($"{name}: Không tìm thấy RunPlanetBuffManager.", this);
        }

        if (runtimeStats == null)
        {
            Debug.LogError($"{name}: Không tìm thấy RuntimeStats.", this);
        }
    }

    private void OnEnable()
    {
        if (runPlanetBuffManager != null)
        {
            runPlanetBuffManager.BuffOwnershipChanged += HandleBuffOwnershipChanged;
        }
    }

    private void OnDisable()
    {
        if (runPlanetBuffManager != null)
        {
            runPlanetBuffManager.BuffOwnershipChanged -= HandleBuffOwnershipChanged;
        }
    }

    public bool EquipBuff(string buffId)
    {
        if (!HasRequiredReferences())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(buffId))
        {
            return false;
        }

        if (IsEquipped(buffId))
        {
            return false;
        }

        if (equippedBuffIds.Count >= maxEquippedBuffs)
        {
            return false;
        }

        if (!runPlanetBuffManager.TryGetOwnedBuff(buffId, out PlanetBuffData buffData))
        {
            return false;
        }

        equippedBuffIds.Add(buffId);
        ApplyBuffModifiers(buffData);
        LoadoutChanged?.Invoke();

        return true;
    }

    public bool UnequipBuff(string buffId)
    {
        if (runtimeStats == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(buffId))
        {
            return false;
        }

        bool removed = equippedBuffIds.Remove(buffId);

        if (!removed)
        {
            return false;
        }

        runtimeStats.RemoveModifiersBySource(CreateSourceId(buffId));
        LoadoutChanged?.Invoke();

        return true;
    }

    public bool IsEquipped(string buffId)
    {
        if (string.IsNullOrWhiteSpace(buffId))
        {
            return false;
        }

        return equippedBuffIds.Contains(buffId);
    }

    public void ClearLoadout()
    {
        if (runtimeStats == null)
        {
            return;
        }

        if (equippedBuffIds.Count == 0)
        {
            return;
        }

        foreach (string buffId in equippedBuffIds)
        {
            runtimeStats.RemoveModifiersBySource(CreateSourceId(buffId));
        }

        equippedBuffIds.Clear();
        LoadoutChanged?.Invoke();
    }

    private void HandleBuffOwnershipChanged(PlanetBuffData buffData, bool isOwned)
    {
        if (buffData == null || isOwned)
        {
            return;
        }

        string buffId = buffData.BuffId;

        if (!equippedBuffIds.Remove(buffId))
        {
            return;
        }

        if (runtimeStats != null)
        {
            runtimeStats.RemoveModifiersBySource(CreateSourceId(buffId));
        }

        LoadoutChanged?.Invoke();
    }

    private void ApplyBuffModifiers(PlanetBuffData buffData)
    {
        string sourceId = CreateSourceId(buffData.BuffId);

        foreach (PlanetBuffStatData statData in buffData.StatModifiers)
        {
            if (statData == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(statData.ModifierId))
            {
                Debug.LogWarning($"{name}: Planet Buff {buffData.DisplayName} có ModifierId trống.", buffData);
                continue;
            }

            StatModifier modifier = new StatModifier(sourceId, statData.ModifierId, statData.StatType, statData.ModifierType, statData.Value);

            runtimeStats.AddOrReplaceModifier(modifier);
        }
    }

    private bool HasRequiredReferences()
    {
        if (runPlanetBuffManager == null)
        {
            Debug.LogError($"{name}: Thiếu RunPlanetBuffManager.", this);
            return false;
        }

        if (runtimeStats == null)
        {
            Debug.LogError($"{name}: Thiếu RuntimeStats.", this);
            return false;
        }

        return true;
    }

    private static string CreateSourceId(string buffId)
    {
        return $"buff:{buffId}";
    }
}