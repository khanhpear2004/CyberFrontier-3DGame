using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemStatData
{
    [SerializeField] private string modifierId;
    [SerializeField] private StatType statType;
    [SerializeField] private StatModifierType modifierType;
    [SerializeField] private float valuePerStack;

    public string ModifierId => modifierId;
    public StatType StatType => statType;
    public StatModifierType ModifierType => modifierType;
    public float ValuePerStack => valuePerStack;
}


[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private int maxStackSize = 1;
    [SerializeField] private List<ItemStatData> statModifiers = new List<ItemStatData>();

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public int MaxStackSize => maxStackSize;
    public IEnumerable<ItemStatData> StatModifiers => statModifiers;
}