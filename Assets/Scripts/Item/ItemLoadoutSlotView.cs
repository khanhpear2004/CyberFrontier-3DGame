using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemLoadoutSlotView : MonoBehaviour
{
    [Header("States")]
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject itemContent;

    [Header("Item Content")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text stackText;

    [Header("Interaction")]
    [SerializeField] private Button button;

    private ItemData boundItem;
    private Action<ItemData> clickedCallback;

    private void Awake()
    {
        ValidateReferences();

        if (button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }

        SetEmpty();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    public void SetEmpty()
    {
        boundItem = null;
        clickedCallback = null;

        if (emptyState != null)
        {
            emptyState.SetActive(true);
        }

        if (itemContent != null)
        {
            itemContent.SetActive(false);
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (stackText != null)
        {
            stackText.text = string.Empty;
        }

        if (button != null)
        {
            button.interactable = false;
        }
    }

    public void Bind(
        ItemData itemData,
        int effectiveStackCount,
        Action<ItemData> onClicked)
    {
        if (itemData == null)
        {
            SetEmpty();
            return;
        }

        boundItem = itemData;
        clickedCallback = onClicked;

        if (emptyState != null)
        {
            emptyState.SetActive(false);
        }

        if (itemContent != null)
        {
            itemContent.SetActive(true);
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = itemData.Icon;
            itemIcon.enabled = itemData.Icon != null;
        }

        if (stackText != null)
        {
            int displayedStackCount = Mathf.Max(0, effectiveStackCount);
            stackText.text = $"x{displayedStackCount}";
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

    private void ValidateReferences()
    {
        if (emptyState == null)
        {
            Debug.LogError($"{name}: EmptyState has not been assigned.", this);
        }

        if (itemContent == null)
        {
            Debug.LogError($"{name}: ItemContent has not been assigned.", this);
        }

        if (itemIcon == null)
        {
            Debug.LogError($"{name}: ItemIcon has not been assigned.", this);
        }

        if (stackText == null)
        {
            Debug.LogError($"{name}: StackText has not been assigned.", this);
        }

        if (button == null)
        {
            Debug.LogError($"{name}: Button has not been assigned.", this);
        }
    }
}