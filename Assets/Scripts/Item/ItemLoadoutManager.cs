using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ItemLoadoutManager : MonoBehaviour
{
    [SerializeField] private RunItemManager runItemManager;
    [SerializeField] private RuntimeStats runtimeStats;

    [Min(1)]
    [SerializeField] private int maxEquippedItems = 10;

    private readonly List<string> equippedItemIds = new List<string>();

    public event Action LoadoutChanged;
    public int MaxEquippedItems => maxEquippedItems;
    public int EquippedCount => equippedItemIds.Count;

    public IReadOnlyList<string> EquippedItemIds => equippedItemIds;

    private void Awake()
    {
        if (runItemManager == null)
        {
            runItemManager = GetComponent<RunItemManager>();
        }

        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<RuntimeStats>();
        }

        if (runItemManager == null)
        {
            Debug.LogError($"{name}: Không tìm thấy RunItemManager.", this);
        }

        if (runtimeStats == null)
        {
            Debug.LogError($"{name}: Không tìm thấy RuntimeStats.", this);
        }
    }

    private void OnEnable()
    {
        if (runItemManager != null)
        {
            runItemManager.ItemStackChanged += HandleItemStackChanged;
        }
    }

    private void OnDisable()
    {
        if (runItemManager != null)
        {
            runItemManager.ItemStackChanged -= HandleItemStackChanged;
        }
    }

    public bool EquipItem(string itemId)
    {
        if (!HasRequiredReferences())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (IsEquipped(itemId))
        {
            return false;
        }

        if (equippedItemIds.Count >= maxEquippedItems)
        {
            return false;
        }

        if (!runItemManager.TryGetOwnedItem(itemId, out ItemData itemData, out int stackCount))
        {
            return false;
        }

        equippedItemIds.Add(itemId);

        ApplyItemModifiers(itemData, stackCount);

        LoadoutChanged?.Invoke();

        return true;
    }

    public bool UnequipItem(string itemId)
    {
        if (runtimeStats == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        bool removed = equippedItemIds.Remove(itemId);

        if (!removed)
        {
            return false;
        }

        runtimeStats.RemoveModifiersBySource(CreateSourceId(itemId));

        LoadoutChanged?.Invoke();

        return true;
    }

    public bool IsEquipped(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        return equippedItemIds.Contains(itemId);
    }

    public void ClearLoadout()
    {
        if (runtimeStats == null)
        {
            return;
        }

        if (equippedItemIds.Count == 0)
        {
            return;
        }

        foreach (string itemId in equippedItemIds)
        {
            runtimeStats.RemoveModifiersBySource(CreateSourceId(itemId));
        }

        equippedItemIds.Clear();

        LoadoutChanged?.Invoke();
    }

    private void HandleItemStackChanged(ItemData itemData, int stackCount)
    {
        if (itemData == null)
        {
            return;
        }

        if (!IsEquipped(itemData.ItemId))
        {
            return;
        }

        if (stackCount <= 0)
        {
            equippedItemIds.Remove(itemData.ItemId);

            runtimeStats.RemoveModifiersBySource(CreateSourceId(itemData.ItemId));

            LoadoutChanged?.Invoke();

            return;
        }

        ApplyItemModifiers(itemData, stackCount);
    }

    private void ApplyItemModifiers(ItemData itemData, int stackCount)
    {
        int effectiveStackCount = Mathf.Clamp(stackCount, 0, itemData.MaxStackSize);
        string sourceId = CreateSourceId(itemData.ItemId);

        foreach (ItemStatData statData in itemData.StatModifiers)
        {
            if (statData == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(statData.ModifierId))
            {
                Debug.LogWarning(
                    $"{name}: Item " +
                    $"{itemData.DisplayName} có " +
                    "ModifierId trống.",
                    itemData
                );

                continue;
            }

            float modifierValue = CalculateModifierValue(statData, effectiveStackCount);
            StatModifier modifier = new StatModifier(sourceId, statData.ModifierId, statData.StatType, statData.ModifierType, modifierValue);

            runtimeStats.AddOrReplaceModifier(modifier);
        }
    }

    private float CalculateModifierValue(ItemStatData statData, int stackCount)
    {
        switch (statData.ModifierType)
        {
            case StatModifierType.Flat:
            case StatModifierType.AdditivePercent: return statData.ValuePerStack* stackCount;
            case StatModifierType.Multiplicative: return Mathf.Pow(statData.ValuePerStack, stackCount);
            default: return 0f;
        }
    }

    private bool HasRequiredReferences()
    {
        if (runItemManager == null)
        {
            Debug.LogError($"{name}: Thiếu RunItemManager.", this);
            return false;
        }

        if (runtimeStats == null)
        {
            Debug.LogError($"{name}: Thiếu RuntimeStats.", this);
            return false;
        }
        return true;
    }

    private string CreateSourceId(string itemId)
    {
        return $"item:{itemId}";
    }
}