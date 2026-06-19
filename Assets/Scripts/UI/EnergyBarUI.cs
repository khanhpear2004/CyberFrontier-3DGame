using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Thanh năng lượng trên HUD. Tự cập nhật khi PlayerEnergy thay đổi (qua sự kiện).
public class EnergyBarUI : MonoBehaviour
{
    [Header("Nguồn dữ liệu")]
    public PlayerEnergy energy;       // Để trống sẽ tự tìm

    [Header("Tham chiếu UI")]
    public Image fillImage;           // Phần thanh (Image Type = Filled, Horizontal)
    public TMP_Text energyText;       // Chữ "150 / 200" (tùy chọn)

    void Start()
    {
        if (energy == null) energy = FindFirstObjectByType<PlayerEnergy>();

        if (energy != null)
        {
            energy.OnEnergyChanged += UpdateBar;
            UpdateBar(energy.currentEnergy, energy.maxEnergy);
        }
    }

    void OnDestroy()
    {
        if (energy != null) energy.OnEnergyChanged -= UpdateBar;
    }

    void UpdateBar(float current, float max)
    {
        if (fillImage != null)
            fillImage.fillAmount = (max > 0f) ? current / max : 0f;

        if (energyText != null)
            energyText.text = Mathf.RoundToInt(current) + " / " + Mathf.RoundToInt(max);
    }
}
