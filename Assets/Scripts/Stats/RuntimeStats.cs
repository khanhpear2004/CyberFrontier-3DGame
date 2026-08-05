using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RuntimeStats : MonoBehaviour
{
    [Serializable]
    private sealed class BaseStatEntry
    {
        [SerializeField] private StatType statType;
        [SerializeField] private float baseValue;
        public StatType StatType => statType;
        public float BaseValue => baseValue;
    }

    [Header("Base Stats")]
    [SerializeField]
    private List<BaseStatEntry> baseStats = new List<BaseStatEntry>();

    private readonly Dictionary<StatType, RuntimeStat> stats = new Dictionary<StatType, RuntimeStat>();

    public event Action<StatType, float, float> StatChanged;

    private void Awake()
    {
        InitializeBaseStats();
    }

    private void InitializeBaseStats()
    {
        stats.Clear();

        foreach (BaseStatEntry entry in baseStats)
        {
            if (entry == null)
            {
                Debug.LogWarning($"{name}: Base Stats có phần tử rỗng và sẽ bị bỏ qua.", this);
                continue;
            }

            if (stats.TryGetValue(entry.StatType, out RuntimeStat existingStat))
            {
                Debug.LogWarning($"{name}: Stat {entry.StatType} bị khai báo trùng. " + $"Giữ giá trị đầu tiên ({existingStat.BaseValue}) " + $"và bỏ qua giá trị {entry.BaseValue}.", this);
                continue;
            }
            
            stats[entry.StatType] = new RuntimeStat(entry.BaseValue);
        }
    }

    public void SetBaseValue( StatType statType, float baseValue)
    {
        RuntimeStat stat = GetOrCreateStat(statType);
        float oldValue = stat.Value;
        stat.SetBaseValue(baseValue);
        NotifyIfChanged(statType, oldValue, stat.Value);
    }

    public float GetBaseValue(StatType statType)
    {
        if (!stats.TryGetValue(statType, out RuntimeStat stat))
        {
            return 0f;
        }

        return stat.BaseValue;
    }

    public float GetValue( StatType statType)
    {
        if (!stats.TryGetValue( statType, out RuntimeStat stat))
        {
            return 0f;
        }

        return stat.Value;
    }

    public void AddOrReplaceModifier(StatModifier modifier)
    {
        if (modifier == null)
        {
            return;
        }

        RuntimeStat stat = GetOrCreateStat( modifier.StatType);

        float oldValue = stat.Value;

        stat.AddOrReplaceModifier(modifier);

        NotifyIfChanged(modifier.StatType, oldValue, stat.Value);
    }

    public bool RemoveModifier(StatType statType, string sourceId, string modifierId)
    {
        if (!stats.TryGetValue(statType, out RuntimeStat stat))
        {
            return false;
        }

        float oldValue = stat.Value;

        bool removed = stat.RemoveModifier(sourceId, modifierId);

        if (removed)
        {
            NotifyIfChanged(statType, oldValue, stat.Value);
        }

        return removed;
    }

    public int RemoveModifiersBySource(string sourceId)
    {
        int totalRemoved = 0;

        foreach (KeyValuePair<StatType, RuntimeStat> pair in stats)
        {
            float oldValue = pair.Value.Value;

            int removedCount = pair.Value.RemoveModifiersBySource( sourceId);

            if (removedCount <= 0)
            {
                continue;
            }

            totalRemoved += removedCount;

            NotifyIfChanged(pair.Key, oldValue, pair.Value.Value);
        }

        return totalRemoved;
    }

    public void ClearAllModifiers()
    {
        foreach (KeyValuePair<StatType, RuntimeStat> pair in stats)
        {
            float oldValue = pair.Value.Value;

            pair.Value.ClearModifiers();

            NotifyIfChanged(pair.Key, oldValue,pair.Value.Value);
        }
    }

    private RuntimeStat GetOrCreateStat(StatType statType)
    {
        if (stats.TryGetValue(statType, out RuntimeStat stat))
        {
            return stat;
        }

        RuntimeStat newStat = new RuntimeStat(0f);

        stats.Add(statType, newStat);

        return newStat;
    }

    private void NotifyIfChanged(StatType statType, float oldValue, float newValue)
    {
        if (Mathf.Approximately(oldValue, newValue))
        {
            return;
        }

        StatChanged?.Invoke(statType, oldValue, newValue);
    }
}
