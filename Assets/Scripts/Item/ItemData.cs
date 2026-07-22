using UnityEngine;

[CreateAssetMenu(
    fileName = "NewItem",
    menuName = "Game/Item"
)]
public class ItemData : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [TextArea]
    [SerializeField] private string description;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public string Description => description;
}