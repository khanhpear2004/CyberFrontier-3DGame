using System;
using UnityEngine;

[Serializable]
public class ItemEntry
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int amount = 1;

    public ItemData Item => item;
    public int Amount => amount;
}