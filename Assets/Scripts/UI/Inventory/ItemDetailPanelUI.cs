using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemDetailPanelUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private RunItemManager runItemManager;
    [SerializeField] private ItemLoadoutManager itemLoadoutManager;

    [Header("Content")]
    [SerializeField] private GameObject selectedItemContent;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text ownedCountText;
    [SerializeField] private TMP_Text effectiveStackText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text effectText;

    [Header("Description Scroll")]
    [SerializeField] private ScrollRect descriptionScrollRect;

    [Header("Action Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonLabel;

    private ItemData selectedItem;

    private void Awake()
    {
        ValidateReferences();

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(HandleActionButtonClicked);
        }

        Clear();
    }

    private void OnEnable()
    {
        if (runItemManager != null)
        {
            runItemManager.ItemStackChanged += HandleItemStackChanged;
        }

        if (itemLoadoutManager != null)
        {
            itemLoadoutManager.LoadoutChanged += HandleLoadoutChanged;
        }
    }

    private void OnDisable()
    {
        if (runItemManager != null)
        {
            runItemManager.ItemStackChanged -= HandleItemStackChanged;
        }

        if (itemLoadoutManager != null)
        {
            itemLoadoutManager.LoadoutChanged -= HandleLoadoutChanged;
        }
    }

    private void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleActionButtonClicked);
        }
    }

    public void ShowItem(ItemData itemData)
    {
        if (itemData == null)
        {
            Clear();
            return;
        }

        selectedItem = itemData;

        if (selectedItemContent != null)
        {
            selectedItemContent.SetActive(true);
        }

        Refresh();
        ResetDescriptionScroll();
    }

    public void Clear()
    {
        selectedItem = null;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (itemNameText != null)
        {
            itemNameText.text = string.Empty;
        }

        if (ownedCountText != null)
        {
            ownedCountText.text = "0";
        }

        if (effectiveStackText != null)
        {
            effectiveStackText.text = "0";
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
        }

        if (effectText != null)
        {
            effectText.text = string.Empty;
        }

        if (actionButton != null)
        {
            actionButton.interactable = false;
        }

        if (selectedItemContent != null)
        {
            selectedItemContent.SetActive(false);
        }
    }

    public void Refresh()
    {
        if (selectedItem == null || runItemManager == null)
        {
            Clear();
            return;
        }

        int ownedCount = runItemManager.GetStackCount(selectedItem.ItemId);

        if (ownedCount <= 0)
        {
            Clear();
            return;
        }

        bool isEquipped = itemLoadoutManager != null &&
                          itemLoadoutManager.IsEquipped(selectedItem.ItemId);

        int effectiveStackCount = isEquipped
            ? Mathf.Clamp(ownedCount, 0, selectedItem.MaxStackSize)
            : 0;

        UpdateBasicInformation(ownedCount, effectiveStackCount);
        UpdateEffectText(effectiveStackCount);
        UpdateActionButton(isEquipped);
    }

    private void UpdateBasicInformation(int ownedCount, int effectiveStackCount)
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = selectedItem.Icon;
            itemIcon.enabled = selectedItem.Icon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = selectedItem.DisplayName;
        }

        if (ownedCountText != null)
        {
            ownedCountText.text = ownedCount.ToString();
        }

        if (effectiveStackText != null)
        {
            effectiveStackText.text = effectiveStackCount.ToString();
        }

        if (descriptionText != null)
        {
            descriptionText.text = selectedItem.Description;
        }
    }

    private void UpdateEffectText(int effectiveStackCount)
    {
        if (effectText == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        bool hasModifier = false;

        foreach (ItemStatData statData in selectedItem.StatModifiers)
        {
            if (statData == null)
            {
                continue;
            }

            if (hasModifier)
            {
                builder.AppendLine();
            }

            builder.Append(FormatModifier(statData, effectiveStackCount));
            hasModifier = true;
        }

        effectText.text = hasModifier
            ? builder.ToString()
            : "This item has no stat effect.";
    }

    private string FormatModifier(ItemStatData statData, int effectiveStackCount)
    {
        string statName = statData.StatType.ToString();
        string perStackValue = FormatValue(statData.ModifierType, statData.ValuePerStack);

        if (effectiveStackCount <= 0)
        {
            return $"{statName}: {perStackValue} per stack";
        }

        float totalValue = CalculateTotalValue(statData, effectiveStackCount);
        string formattedTotal = FormatValue(statData.ModifierType, totalValue);

        return $"{statName}: {perStackValue} per stack → {formattedTotal} active";
    }

    private float CalculateTotalValue(ItemStatData statData, int stackCount)
    {
        switch (statData.ModifierType)
        {
            case StatModifierType.Flat:
            case StatModifierType.AdditivePercent:
                return statData.ValuePerStack * stackCount;

            case StatModifierType.Multiplicative:
                return Mathf.Pow(statData.ValuePerStack, stackCount);

            default:
                return 0f;
        }
    }

    private string FormatValue(StatModifierType modifierType, float value)
    {
        switch (modifierType)
        {
            case StatModifierType.Flat:
                return value >= 0f ? $"+{value:0.##}" : $"{value:0.##}";

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

    private void UpdateActionButton(bool isEquipped)
    {
        if (actionButton == null || actionButtonLabel == null)
        {
            return;
        }

        if (itemLoadoutManager == null)
        {
            actionButton.interactable = false;
            actionButtonLabel.text = "Unavailable";
            return;
        }

        if (isEquipped)
        {
            actionButton.interactable = true;
            actionButtonLabel.text = "Unequip";
            return;
        }

        bool hasFreeSlot =
            itemLoadoutManager.EquippedCount < itemLoadoutManager.MaxEquippedItems;

        actionButton.interactable = hasFreeSlot;
        actionButtonLabel.text = hasFreeSlot ? "Equip" : "Slots Full";
    }

    private void HandleActionButtonClicked()
    {
        if (selectedItem == null || itemLoadoutManager == null)
        {
            return;
        }

        bool isEquipped = itemLoadoutManager.IsEquipped(selectedItem.ItemId);

        if (isEquipped)
        {
            itemLoadoutManager.UnequipItem(selectedItem.ItemId);
        }
        else
        {
            itemLoadoutManager.EquipItem(selectedItem.ItemId);
        }

        Refresh();
    }

    private void HandleItemStackChanged(ItemData itemData, int stackCount)
    {
        if (selectedItem == null || itemData == null)
        {
            return;
        }

        if (itemData.ItemId != selectedItem.ItemId)
        {
            return;
        }

        if (stackCount <= 0)
        {
            Clear();
            return;
        }

        Refresh();
    }

    private void HandleLoadoutChanged()
    {
        if (selectedItem != null)
        {
            Refresh();
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
        if (runItemManager == null)
        {
            Debug.LogError($"{name}: RunItemManager has not been assigned.", this);
        }

        if (itemLoadoutManager == null)
        {
            Debug.LogError($"{name}: ItemLoadoutManager has not been assigned.", this);
        }

        if (selectedItemContent == null)
        {
            Debug.LogError($"{name}: SelectedItemContent has not been assigned.", this);
        }

        if (actionButton == null)
        {
            Debug.LogError($"{name}: ActionButton has not been assigned.", this);
        }
    }
}