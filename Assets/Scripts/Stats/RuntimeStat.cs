using System.Collections.Generic;

public sealed class RuntimeStat
{
    private readonly List<StatModifier> modifiers = new List<StatModifier>();

    public float BaseValue { get; private set; }

    public float Value => CalculateValue();

    public IReadOnlyList<StatModifier> Modifiers => modifiers;

    public RuntimeStat(float baseValue)
    {
        BaseValue = baseValue;
    }

    public void SetBaseValue(float baseValue)
    {
        BaseValue = baseValue;
    }

    public void AddOrReplaceModifier(StatModifier modifier)
    {
        if (modifier == null)
        {
            return;
        }

        int existingIndex = modifiers.FindIndex(existing => existing.HasSameIdentity(modifier));

        if (existingIndex >= 0)
        {
            modifiers[existingIndex] = modifier;
        }
        else
        {
            modifiers.Add(modifier);
        }
    }

    public bool RemoveModifier(string sourceId, string modifierId)
    {
        int removedCount = modifiers.RemoveAll( modifier => modifier.Matches(sourceId, modifierId));
        return removedCount > 0;
    }

    public int RemoveModifiersBySource(string sourceId)
    {
        return modifiers.RemoveAll( modifier => modifier.SourceId == sourceId);
    }

    public void ClearModifiers()
    {
        modifiers.Clear();
    }

    private float CalculateValue()
    {
        float totalFlat = 0f;
        float totalAdditivePercent = 0f;
        float totalMultiplier = 1f;

        foreach (StatModifier modifier in modifiers)
        {
            switch (modifier.ModifierType)
            {
                case StatModifierType.Flat: totalFlat += modifier.Value; break;
                case StatModifierType.AdditivePercent: totalAdditivePercent += modifier.Value; break;
                case StatModifierType.Multiplicative: totalMultiplier *= modifier.Value; break;
            }
        }

        float valueAfterFlat = BaseValue + totalFlat;

        float valueAfterPercent = valueAfterFlat * (1f + totalAdditivePercent);

        return valueAfterPercent * totalMultiplier;
    }
}