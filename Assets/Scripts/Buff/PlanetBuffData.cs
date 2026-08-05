using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PlanetBuffStatData
{
    [SerializeField] private string modifierId;
    [SerializeField] private StatType statType;
    [SerializeField] private StatModifierType modifierType;
    [SerializeField] private float value;

    public string ModifierId => modifierId;
    public StatType StatType => statType;
    public StatModifierType ModifierType => modifierType;
    public float Value => value;
}

[CreateAssetMenu(fileName = "NewPlanetBuff", menuName = "Game/Planet Buff")]
public sealed class PlanetBuffData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string buffId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [TextArea(3, 6)]
    [SerializeField] private string description;

    [Header("Stat Modifiers")]
    [SerializeField] private List<PlanetBuffStatData> statModifiers =
        new List<PlanetBuffStatData>();

    public string BuffId => buffId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public IEnumerable<PlanetBuffStatData> StatModifiers => statModifiers;
}