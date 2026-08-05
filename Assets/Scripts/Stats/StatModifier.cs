using System;

[Serializable]
public sealed class StatModifier
{
    public string SourceId { get; }

    public string ModifierId { get; }

    public StatType StatType { get; }

    public StatModifierType ModifierType { get; }

    public float Value { get; }

    public StatModifier(string sourceId, string modifierId, StatType statType, StatModifierType modifierType, float value)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            throw new ArgumentException("SourceId không được để trống.", nameof(sourceId));
        }

        if (string.IsNullOrWhiteSpace(modifierId))
        {
            throw new ArgumentException( "ModifierId không được để trống.", nameof(modifierId));
        }

        SourceId = sourceId;
        ModifierId = modifierId;
        StatType = statType;
        ModifierType = modifierType;
        Value = value;
    }

    public bool HasSameIdentity(StatModifier other)
    {
        if (other == null)
        {
            return false;
        }

        return SourceId == other.SourceId && ModifierId == other.ModifierId;
    }

    public bool Matches(string sourceId, string modifierId)
    {
        return SourceId == sourceId && ModifierId == modifierId;
    }
}