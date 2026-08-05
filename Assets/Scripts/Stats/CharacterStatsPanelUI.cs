using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterStatsPanelUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private RuntimeStats runtimeStats;

    [Header("UI References")]
    [SerializeField] private Transform content;
    [SerializeField] private CharacterStatRowView rowPrefab;

    private readonly Dictionary<StatType, CharacterStatRowView> rows =
        new Dictionary<StatType, CharacterStatRowView>();

    private void Awake()
    {
        BuildRows();
    }

    private void Start()
    {
        RefreshAll();
    }

    private void OnEnable()
    {
        if (runtimeStats == null)
        {
            return;
        }

        runtimeStats.StatChanged += HandleStatChanged;
        RefreshAll();
    }

    private void OnDisable()
    {
        if (runtimeStats == null)
        {
            return;
        }

        runtimeStats.StatChanged -= HandleStatChanged;
    }

    private void BuildRows()
    {
        if (content == null)
        {
            Debug.LogError($"{name}: Character stats Content chưa được gắn.", this);
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogError($"{name}: Character stat Row Prefab chưa được gắn.", this);
            return;
        }

        rows.Clear();

        StatType[] statTypes = (StatType[])Enum.GetValues(typeof(StatType));

        foreach (StatType statType in statTypes)
        {
            CharacterStatRowView row = Instantiate(rowPrefab, content);
            row.gameObject.SetActive(true);

            float value = runtimeStats != null ? runtimeStats.GetValue(statType) : 0f;

            row.Bind(statType, value);
            rows.Add(statType, row);
        }
    }

    private void RefreshAll()
    {
        if (runtimeStats == null)
        {
            return;
        }

        foreach (KeyValuePair<StatType, CharacterStatRowView> pair in rows)
        {
            float value = runtimeStats.GetValue(pair.Key);
            pair.Value.SetValue(value);
        }
    }

    private void HandleStatChanged(StatType statType, float oldValue, float newValue)
    {
        if (!rows.TryGetValue(statType, out CharacterStatRowView row))
        {
            return;
        }

        row.SetValue(newValue);
    }
}