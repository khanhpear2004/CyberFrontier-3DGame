using TMPro;
using UnityEngine;

public sealed class CharacterStatRowView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text statNameText;
    [SerializeField] private TMP_Text statValueText;

    public StatType StatType { get; private set; }

    public void Bind(StatType statType, float value)
    {
        StatType = statType;

        if (statNameText != null)
        {
            statNameText.text = GetDisplayName(statType);
        }

        SetValue(value);
    }

    public void SetValue(float value)
    {
        if (statValueText == null)
        {
            return;
        }

        statValueText.text = value.ToString("0.##");
    }

    private static string GetDisplayName(StatType statType)
    {
        switch (statType)
        {
            case StatType.Damage:
                return "Damage";

            case StatType.FireRate:
                return "Fire Rate";

            case StatType.ProjectileSpeed:
                return "Projectile Speed";

            case StatType.MaxHealth:
                return "Max Health";

            case StatType.DamageTaken:
                return "Damage Taken";

            case StatType.WalkSpeed:
                return "Walk Speed";

            case StatType.RunSpeed:
                return "Run Speed";

            case StatType.FlySpeed:
                return "Fly Speed";

            case StatType.MaxEnergy:
                return "Max Energy";

            case StatType.EnergyRegen:
                return "Energy Regen";

            case StatType.EnergyCost:
                return "Energy Cost";

            case StatType.SkillDamage:
                return "Skill Damage";

            case StatType.SkillCooldown:
                return "Skill Cooldown";

            default:
                return statType.ToString();
        }
    }
}