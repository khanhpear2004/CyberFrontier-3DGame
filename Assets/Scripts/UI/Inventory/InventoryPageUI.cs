using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InventoryPageUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private RunItemManager runItemManager;
    [SerializeField] private ItemLoadoutManager itemLoadoutManager;

    [Header("Inventory List")]
    [SerializeField] private RectTransform inventoryContent;
    [SerializeField] private InventoryItemView itemViewPrefab;

    [Header("Item Detail")]
    [SerializeField] private ItemDetailPanelUI itemDetailPanel;

    private readonly List<InventoryItemView> spawnedItemViews = new List<InventoryItemView>();

    private string selectedItemId;

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        RebuildInventory();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    public void RebuildInventory()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        List<ItemData> ownedItems = GetSortedOwnedItems();

        ClearSpawnedItemViews();
        CreateItemViews(ownedItems);
        RestoreSelection(ownedItems);
    }

    private List<ItemData> GetSortedOwnedItems()
    {
        List<ItemData> ownedItems = new List<ItemData>();

        foreach (ItemData itemData in runItemManager.GetOwnedItems())
        {
            if (itemData == null)
            {
                continue;
            }

            int stackCount = runItemManager.GetStackCount(itemData.ItemId);

            if (stackCount <= 0)
            {
                continue;
            }

            ownedItems.Add(itemData);
        }

        ownedItems.Sort(CompareItems);
        return ownedItems;
    }

    private int CompareItems(ItemData firstItem, ItemData secondItem)
    {
        int nameComparison = string.Compare(firstItem.DisplayName, secondItem.DisplayName, StringComparison.OrdinalIgnoreCase);

        if (nameComparison != 0)
        {
            return nameComparison;
        }

        return string.Compare(firstItem.ItemId, secondItem.ItemId, StringComparison.Ordinal);
    }

    private void CreateItemViews(List<ItemData> ownedItems)
    {
        foreach (ItemData itemData in ownedItems)
        {
            int stackCount = runItemManager.GetStackCount(itemData.ItemId);
            bool isEquipped = itemLoadoutManager.IsEquipped(itemData.ItemId);

            InventoryItemView itemView = Instantiate(itemViewPrefab, inventoryContent);

            itemView.Bind(itemData, stackCount, isEquipped, HandleItemClicked);

            spawnedItemViews.Add(itemView);
        }
    }

    private void RestoreSelection(List<ItemData> ownedItems)
    {
        if (ownedItems.Count == 0)
        {
            selectedItemId = null;
            itemDetailPanel.Clear();
            return;
        }

        ItemData itemToSelect = FindPreviouslySelectedItem(ownedItems);

        if (itemToSelect == null)
        {
            itemToSelect = ownedItems[0];
        }

        SelectItem(itemToSelect);
    }

    private ItemData FindPreviouslySelectedItem(List<ItemData> ownedItems)
    {
        if (string.IsNullOrWhiteSpace(selectedItemId))
        {
            return null;
        }

        foreach (ItemData itemData in ownedItems)
        {
            if (itemData.ItemId == selectedItemId)
            {
                return itemData;
            }
        }

        return null;
    }

    private void SelectItem(ItemData itemData)
    {
        if (itemData == null)
        {
            selectedItemId = null;
            itemDetailPanel.Clear();
            return;
        }

        selectedItemId = itemData.ItemId;
        itemDetailPanel.ShowItem(itemData);
    }

    private void HandleItemClicked(ItemData itemData)
    {
        SelectItem(itemData);
    }

    private void HandleItemStackChanged(ItemData itemData, int stackCount)
    {
        RebuildInventory();
    }

    private void HandleLoadoutChanged()
    {
        RebuildInventory();
    }

    private void ClearSpawnedItemViews()
    {
        foreach (InventoryItemView itemView in spawnedItemViews)
        {
            if (itemView == null)
            {
                continue;
            }

            itemView.gameObject.SetActive(false);
            Destroy(itemView.gameObject);
        }

        spawnedItemViews.Clear();
    }

    private void SubscribeEvents()
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

    private void UnsubscribeEvents()
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

    private bool HasRequiredReferences()
    {
        return runItemManager != null &&
               itemLoadoutManager != null &&
               inventoryContent != null &&
               itemViewPrefab != null &&
               itemDetailPanel != null;
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

        if (inventoryContent == null)
        {
            Debug.LogError($"{name}: InventoryContent has not been assigned.", this);
        }

        if (itemViewPrefab == null)
        {
            Debug.LogError($"{name}: ItemViewPrefab has not been assigned.", this);
        }

        if (itemDetailPanel == null)
        {
            Debug.LogError($"{name}: ItemDetailPanelUI has not been assigned.", this);
        }
    }
}