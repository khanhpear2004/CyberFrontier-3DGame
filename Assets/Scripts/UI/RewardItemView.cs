using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text amountText;

    public void Bind(ItemData item, int amount)
    {
        iconImage.sprite = item.Icon;
        iconImage.enabled = item.Icon != null;

        itemNameText.text = item.DisplayName;
        amountText.text = $"x{amount}";
    }
}