using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text stackText;
    [SerializeField] private GameObject equippedMarker;
    [SerializeField] private Button button;

    private ItemData boundItem;
    private Action<ItemData> clickedCallback;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(
                HandleClicked
            );
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleClicked
            );
        }
    }

    public void Bind(ItemData itemData, int stackCount, bool isEquipped, Action<ItemData> onClicked)
    {
        boundItem = itemData;
        clickedCallback = onClicked;

        iconImage.sprite = itemData.Icon;
        iconImage.enabled = itemData.Icon != null;

        if (itemNameText != null)
        {
            itemNameText.text = itemData.DisplayName;
        }

        stackText.text = $"x{stackCount}";

        if (equippedMarker != null)
        {
            equippedMarker.SetActive(isEquipped);
        }

        if (button != null)
        {
            button.interactable = true;
        }
    }

    private void HandleClicked()
    {
        if (boundItem == null)
        {
            return;
        }

        clickedCallback?.Invoke(boundItem);
    }
}