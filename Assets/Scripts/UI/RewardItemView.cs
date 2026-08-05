using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text amountText;

    private ItemData boundItem;
    private int boundAmount;
    private Action<ItemData, int> selectedCallback;

    public void Bind(ItemData item, int amount)
    {
        iconImage.sprite = item.Icon;
        iconImage.enabled = item.Icon != null;

        itemNameText.text = item.DisplayName;

        if (amountText != null)
        {
            amountText.gameObject.SetActive(true);
            amountText.text = $"x{amount}";
        }
    }

    public void Bind(PlanetBuffData buffData)
    {
        iconImage.sprite = buffData.Icon;
        iconImage.enabled = buffData.Icon != null;

        itemNameText.text = buffData.DisplayName;

        if (amountText != null)
        {
            amountText.text = string.Empty;
            amountText.gameObject.SetActive(false);
        }
    }
}
