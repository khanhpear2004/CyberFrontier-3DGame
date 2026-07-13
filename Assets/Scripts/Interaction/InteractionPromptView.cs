using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InteractionPromptView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform promptRect;
    [SerializeField] private Image background;
    [SerializeField] private Image keyBackground;
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private TMP_Text actionText;

    [Header("Colors")]
    [SerializeField] private Color selectedColor =
        new Color(0.04f, 0.18f, 0.32f, 0.95f);

    [SerializeField] private Color normalColor =
        new Color(0.03f, 0.04f, 0.06f, 0.75f);

    [SerializeField] private Color selectedTextColor = Color.white;

    [SerializeField] private Color normalTextColor =
        new Color(1f, 1f, 1f, 0.65f);

    public RectTransform PromptRect
    {
        get
        {
            if (promptRect == null)
            {
                promptRect = transform as RectTransform;
            }

            return promptRect;
        }
    }

    public void SetContent(
        string key,
        string action,
        bool isSelected)
    {
        if (keyText != null)
        {
            keyText.text = key;
        }

        if (actionText != null)
        {
            actionText.text = action;
            actionText.color = isSelected
                ? selectedTextColor
                : normalTextColor;
        }

        if (background != null)
        {
            background.color = isSelected
                ? selectedColor
                : normalColor;
        }

        if (keyBackground != null)
        {
            keyBackground.gameObject.SetActive(isSelected);
        }
    }
}