using System.Collections.Generic;
using UnityEngine;

public sealed class ItemLoadoutPageUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private RunItemManager runItemManager;
    [SerializeField] private ItemLoadoutManager itemLoadoutManager;

    [Header("Slot Container")]
    [SerializeField] private Transform itemSlotContainer;

    private ItemLoadoutSlotView[] itemSlots;

    private void Awake()
    {
        CollectItemSlots();
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (itemLoadoutManager != null)
        {
            itemLoadoutManager.LoadoutChanged += HandleLoadoutChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (itemLoadoutManager != null)
        {
            itemLoadoutManager.LoadoutChanged -= HandleLoadoutChanged;
        }
    }

    public void Refresh()
    {
        ClearAllSlots();

        if (!HasRequiredReferences())
        {
            return;
        }

        IReadOnlyList<string> equippedItemIds =
            itemLoadoutManager.EquippedItemIds;

        int displayedItemCount = Mathf.Min(
            equippedItemIds.Count,
            itemSlots.Length
        );

        for (int i = 0; i < displayedItemCount; i++)
        {
            string itemId = equippedItemIds[i];

            bool foundItem = runItemManager.TryGetOwnedItem(
                itemId,
                out ItemData itemData,
                out int ownedStackCount
            );

            if (!foundItem || itemData == null)
            {
                continue;
            }

            int effectiveStackCount = Mathf.Clamp(
                ownedStackCount,
                0,
                itemData.MaxStackSize
            );

            itemSlots[i].Bind(
                itemData,
                effectiveStackCount,
                HandleSlotClicked
            );
        }
    }

    private void CollectItemSlots()
    {
        if (itemSlotContainer == null)
        {
            itemSlots = new ItemLoadoutSlotView[0];
            return;
        }

        itemSlots = itemSlotContainer.GetComponentsInChildren<
            ItemLoadoutSlotView
        >(true);
    }

    private void ClearAllSlots()
    {
        if (itemSlots == null)
        {
            return;
        }

        foreach (ItemLoadoutSlotView itemSlot in itemSlots)
        {
            if (itemSlot != null)
            {
                itemSlot.SetEmpty();
            }
        }
    }

    private void HandleSlotClicked(ItemData itemData)
    {
        if (itemData == null || itemLoadoutManager == null)
        {
            return;
        }

        itemLoadoutManager.UnequipItem(itemData.ItemId);
    }

    private void HandleLoadoutChanged()
    {
        Refresh();
    }

    private bool HasRequiredReferences()
    {
        return runItemManager != null &&
               itemLoadoutManager != null &&
               itemSlots != null &&
               itemSlots.Length > 0;
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

        if (itemSlotContainer == null)
        {
            Debug.LogError($"{name}: ItemSlotContainer has not been assigned.", this);
            return;
        }

        if (itemSlots == null || itemSlots.Length == 0)
        {
            Debug.LogError($"{name}: No ItemLoadoutSlotView was found.", this);
            return;
        }

        if (itemLoadoutManager != null &&
            itemSlots.Length != itemLoadoutManager.MaxEquippedItems)
        {
            Debug.LogWarning(
                $"{name}: Found {itemSlots.Length} item slots, but " +
                $"ItemLoadoutManager allows {itemLoadoutManager.MaxEquippedItems}.",
                this
            );
        }
    }
}