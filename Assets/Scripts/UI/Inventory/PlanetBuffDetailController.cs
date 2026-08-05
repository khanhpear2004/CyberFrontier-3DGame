using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlanetBuffDetailController : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private RunPlanetBuffManager runPlanetBuffManager;
    [SerializeField] private PlanetBuffLoadoutManager buffLoadoutManager;

    [Header("Right Panels")]
    [SerializeField] private GameObject characterStatPanel;
    [SerializeField] private GameObject planetBuffDetailPanel;

    [Header("Buff Information")]
    [SerializeField] private Image buffIcon;
    [SerializeField] private TMP_Text buffNameText;
    [SerializeField] private TMP_Text equippedStatusText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Description Scroll")]
    [SerializeField] private ScrollRect descriptionScrollRect;

    [Header("Buttons")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonLabel;
    [SerializeField] private Button backButton;

    private PlanetBuffData selectedBuff;

    private void Awake()
    {
        ValidateReferences();

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(HandleActionButtonClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(ShowCharacterStats);
        }

        ShowCharacterStats();
    }

    private void OnEnable()
    {
        if (runPlanetBuffManager != null)
        {
            runPlanetBuffManager.BuffOwnershipChanged += HandleBuffOwnershipChanged;
        }

        if (buffLoadoutManager != null)
        {
            buffLoadoutManager.LoadoutChanged += HandleLoadoutChanged;
        }

        ShowCharacterStats();
    }

    private void OnDisable()
    {
        if (runPlanetBuffManager != null)
        {
            runPlanetBuffManager.BuffOwnershipChanged -= HandleBuffOwnershipChanged;
        }

        if (buffLoadoutManager != null)
        {
            buffLoadoutManager.LoadoutChanged -= HandleLoadoutChanged;
        }
    }

    private void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleActionButtonClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ShowCharacterStats);
        }
    }

    public void ShowBuff(PlanetBuffData buffData)
    {
        if (buffData == null)
        {
            ShowCharacterStats();
            return;
        }

        selectedBuff = buffData;

        if (characterStatPanel != null)
        {
            characterStatPanel.SetActive(false);
        }

        if (planetBuffDetailPanel != null)
        {
            planetBuffDetailPanel.SetActive(true);
        }

        RefreshDetail();
        ResetDescriptionScroll();
    }

    public void ShowCharacterStats()
    {
        selectedBuff = null;

        if (planetBuffDetailPanel != null)
        {
            planetBuffDetailPanel.SetActive(false);
        }

        if (characterStatPanel != null)
        {
            characterStatPanel.SetActive(true);
        }
    }

    private void RefreshDetail()
    {
        if (selectedBuff == null)
        {
            return;
        }

        UpdateBasicInformation();
        UpdateEffectText();
        UpdateActionButton();
    }

    private void UpdateBasicInformation()
    {
        if (buffIcon != null)
        {
            buffIcon.sprite = selectedBuff.Icon;
            buffIcon.enabled = selectedBuff.Icon != null;
            buffIcon.preserveAspect = true;
        }

        if (buffNameText != null)
        {
            buffNameText.text = selectedBuff.DisplayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = selectedBuff.Description;
        }

        bool isEquipped = buffLoadoutManager != null &&
                          buffLoadoutManager.IsEquipped(selectedBuff.BuffId);

        if (equippedStatusText != null)
        {
            equippedStatusText.text = isEquipped
                ? "Status: Equipped"
                : "Status: Not Equipped";
        }
    }

    private void UpdateEffectText()
    {
        if (effectText == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        bool hasModifier = false;

        foreach (PlanetBuffStatData statData in selectedBuff.StatModifiers)
        {
            if (statData == null)
            {
                continue;
            }

            if (hasModifier)
            {
                builder.AppendLine();
            }

            builder.Append(FormatModifier(statData));
            hasModifier = true;
        }

        effectText.text = hasModifier
            ? builder.ToString()
            : "This buff has no stat effect.";
    }

    private string FormatModifier(PlanetBuffStatData statData)
    {
        string statName = FormatStatName(statData.StatType);
        string value = FormatValue(statData.ModifierType, statData.Value);

        return $"{statName}: {value}";
    }

    private string FormatValue(StatModifierType modifierType, float value)
    {
        switch (modifierType)
        {
            case StatModifierType.Flat:
                return value >= 0f
                    ? $"+{value:0.##}"
                    : $"{value:0.##}";

            case StatModifierType.AdditivePercent:
                float percentValue = value * 100f;

                return percentValue >= 0f
                    ? $"+{percentValue:0.##}%"
                    : $"{percentValue:0.##}%";

            case StatModifierType.Multiplicative:
                return $"x{value:0.##}";

            default:
                return value.ToString("0.##");
        }
    }

    private string FormatStatName(StatType statType)
    {
        switch (statType)
        {
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

    private void UpdateActionButton()
    {
        if (actionButton == null || actionButtonLabel == null)
        {
            return;
        }

        if (selectedBuff == null ||
            runPlanetBuffManager == null ||
            buffLoadoutManager == null)
        {
            actionButton.interactable = false;
            actionButtonLabel.text = "Unavailable";
            return;
        }

        bool isOwned = runPlanetBuffManager.HasBuff(selectedBuff.BuffId);

        if (!isOwned)
        {
            actionButton.interactable = false;
            actionButtonLabel.text = "Unavailable";
            return;
        }

        bool isEquipped = buffLoadoutManager.IsEquipped(selectedBuff.BuffId);

        if (isEquipped)
        {
            actionButton.interactable = true;
            actionButtonLabel.text = "Unequip";
            return;
        }

        bool hasFreeSlot =
            buffLoadoutManager.EquippedCount <
            buffLoadoutManager.MaxEquippedBuffs;

        actionButton.interactable = hasFreeSlot;
        actionButtonLabel.text = hasFreeSlot ? "Equip" : "Slots Full";
    }

    private void HandleActionButtonClicked()
    {
        if (selectedBuff == null || buffLoadoutManager == null)
        {
            return;
        }

        bool isEquipped =
            buffLoadoutManager.IsEquipped(selectedBuff.BuffId);

        if (isEquipped)
        {
            buffLoadoutManager.UnequipBuff(selectedBuff.BuffId);
        }
        else
        {
            buffLoadoutManager.EquipBuff(selectedBuff.BuffId);
        }

        RefreshDetail();
    }

    private void HandleBuffOwnershipChanged(
        PlanetBuffData buffData,
        bool isOwned)
    {
        if (selectedBuff == null || buffData == null)
        {
            return;
        }

        if (buffData.BuffId != selectedBuff.BuffId)
        {
            return;
        }

        if (!isOwned)
        {
            ShowCharacterStats();
            return;
        }

        RefreshDetail();
    }

    private void HandleLoadoutChanged()
    {
        if (selectedBuff != null)
        {
            RefreshDetail();
        }
    }

    private void ResetDescriptionScroll()
    {
        if (descriptionScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        descriptionScrollRect.verticalNormalizedPosition = 1f;
    }

    private void ValidateReferences()
    {
        if (runPlanetBuffManager == null)
        {
            Debug.LogError(
                $"{name}: RunPlanetBuffManager has not been assigned.",
                this
            );
        }

        if (buffLoadoutManager == null)
        {
            Debug.LogError(
                $"{name}: PlanetBuffLoadoutManager has not been assigned.",
                this
            );
        }

        if (characterStatPanel == null)
        {
            Debug.LogError(
                $"{name}: CharacterStatPanel has not been assigned.",
                this
            );
        }

        if (planetBuffDetailPanel == null)
        {
            Debug.LogError(
                $"{name}: PlanetBuffDetailPanel has not been assigned.",
                this
            );
        }

        if (actionButton == null)
        {
            Debug.LogError(
                $"{name}: ActionButton has not been assigned.",
                this
            );
        }

        if (backButton == null)
        {
            Debug.LogError(
                $"{name}: BackButton has not been assigned.",
                this
            );
        }
    }
}