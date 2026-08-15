using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsSliderRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text valueText;

    public Slider Slider => slider;

    public void Configure(
        TMP_Text rowLabel,
        Slider rowSlider,
        TMP_Text rowValue,
        string displayName,
        float initialValue
    )
    {
        labelText = rowLabel;
        slider = rowSlider;
        valueText = rowValue;

        labelText.text = displayName;
        slider.SetValueWithoutNotify(Mathf.Clamp01(initialValue));
        RefreshValue(slider.value);
    }

    private void OnEnable()
    {
        if (slider == null)
        {
            return;
        }

        slider.onValueChanged.AddListener(RefreshValue);
        RefreshValue(slider.value);
    }

    private void OnDisable()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(RefreshValue);
        }
    }

    private void RefreshValue(float value)
    {
        if (valueText != null)
        {
            valueText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }
    }

    public void Refresh()
    {
        if (slider != null)
        {
            RefreshValue(slider.value);
        }
    }
}
