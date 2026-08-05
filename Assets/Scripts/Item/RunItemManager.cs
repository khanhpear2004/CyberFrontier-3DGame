using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunItemManager : MonoBehaviour
{
    private sealed class OwnedItem
    {
        public ItemData ItemData { get; }
        public int StackCount { get; set; }

        public OwnedItem(ItemData itemData)
        {
            ItemData = itemData;
            StackCount = 1;
        }
    }

    //[SerializeField] private RuntimeStats runtimeStats;

    private readonly Dictionary<string, OwnedItem> ownedItems = new Dictionary<string, OwnedItem>();
    public event Action<ItemData, int> ItemStackChanged;

    
    public bool AddItem(ItemData itemData)
    {
        if (!CanUseItem(itemData))
        {
            return false;
        }

        string itemId = itemData.ItemId;

        if (ownedItems.TryGetValue(itemId, out OwnedItem ownedItem))
        {
            if (ownedItem.ItemData != itemData)
            {
                Debug.LogWarning($"{name}: Có hai ItemData khác nhau cùng ItemId {itemId}.", this);
                return false;
            }

            ownedItem.StackCount++;
        }
        else
        {
            ownedItem = new OwnedItem(itemData);
            ownedItems.Add(itemId, ownedItem);
        }

        ItemStackChanged?.Invoke(ownedItem.ItemData, ownedItem.StackCount);
        return true;
    }

    public bool RemoveItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (!ownedItems.TryGetValue(itemId, out OwnedItem ownedItem))
        {
            return false;
        }

        ownedItem.StackCount--;

        if (ownedItem.StackCount <= 0)
        {
            ownedItems.Remove(itemId);

            ItemStackChanged?.Invoke(ownedItem.ItemData, 0);

            return true;
        }
    
        ItemStackChanged?.Invoke(ownedItem.ItemData, ownedItem.StackCount);
        return true;
    }

    public bool HasItem(string itemId)
    {
        return GetStackCount(itemId) > 0;
    }

    public int GetStackCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        if (!ownedItems.TryGetValue(itemId, out OwnedItem ownedItem))
        {
            return 0;
        }

        return ownedItem.StackCount;
    }

    public void ClearAllItems()
    {
        if (ownedItems.Count == 0) return;

        List<ItemData> removedItems = new List<ItemData>();

        foreach (OwnedItem ownedItem in ownedItems.Values)
        {
            removedItems.Add(ownedItem.ItemData);
        }

        ownedItems.Clear();

        foreach (ItemData itemData in removedItems)
        {
            ItemStackChanged?.Invoke(itemData, 0);
        }
    }

    private bool CanUseItem(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning($"{name}: Không thể thêm ItemData null.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(itemData.ItemId))
        {
            Debug.LogWarning($"{name}: Item {itemData.name} chưa có ItemId.", itemData);
            return false;
        }

        if (itemData.MaxStackSize <= 0)
        {
            Debug.LogWarning($"{name}: Item {itemData.name} có MaxStackSize không hợp lệ.", itemData);
            return false;
        }

        return true;
    }

    public bool TryGetOwnedItem(string itemId, out ItemData itemData, out int stackCount)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemData = null;
            stackCount = 0;
            return false;
        }

        if (!ownedItems.TryGetValue(itemId, out OwnedItem ownedItem))
        {
            itemData = null;
            stackCount = 0;
            return false;
        }

        itemData = ownedItem.ItemData;
        stackCount = ownedItem.StackCount;
        return true;
    }

    public IEnumerable<ItemData> GetOwnedItems()
    {
        foreach (OwnedItem ownedItem in ownedItems.Values)
        {
            yield return ownedItem.ItemData;
        }
    }


}