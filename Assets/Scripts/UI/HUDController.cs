using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Thanh máu kiểu hành động (ZZZ):
// - Có "vệt mất máu" (damage trail) chạy đuổi theo khi mất máu
// - Đổi màu theo lượng máu (Gradient)
// - Hiển thị số máu
// - Có phím TEST để kiểm tra máu bị trừ
public class HUDController : MonoBehaviour
{
    [Header("Tham chiếu thanh máu")]
    public Image fillImage;          // Phần máu chính (Image Type = Filled, Horizontal)
    public Image delayedFillImage;   // Vệt mất máu chạy theo sau (tùy chọn)
    public TMP_Text healthText;      // Chữ "80 / 100" (tùy chọn)

    [Header("Chỉ số máu")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Hiệu ứng")]
    public Gradient healthGradient;  // Trái = máu thấp (đỏ) -> Phải = máu đầy (xanh)
    public float trailSpeed = 0.5f;  // Tốc độ vệt mất máu đuổi theo

    [Header("Phím TEST (tắt khi làm thật)")]
    public bool enableTestKeys = true;
    public KeyCode damageKey = KeyCode.K;   // Bấm K = mất máu
    public KeyCode healKey = KeyCode.L;     // Bấm L = hồi máu
    public int testAmount = 10;

    void Start()
    {
        RefreshBar(instant: true);
    }

    void Update()
    {
        // --- Phím test: cho bạn thấy thanh máu phản ứng ---
        if (enableTestKeys)
        {
            if (Input.GetKeyDown(damageKey)) TakeDamage(testAmount);
            if (Input.GetKeyDown(healKey)) Heal(testAmount);
        }

        // --- Vệt mất máu từ từ đuổi theo phần máu chính ---
        if (delayedFillImage != null && fillImage != null)
        {
            if (delayedFillImage.fillAmount > fillImage.fillAmount)
            {
                delayedFillImage.fillAmount = Mathf.MoveTowards(
                    delayedFillImage.fillAmount,
                    fillImage.fillAmount,
                    trailSpeed * Time.deltaTime);
            }
            else
            {
                delayedFillImage.fillAmount = fillImage.fillAmount;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        RefreshBar(); // máu chính tụt ngay, vệt sẽ đuổi theo sau (tạo hiệu ứng)
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        RefreshBar(instant: true); // hồi máu thì cho cả vệt nhảy lên ngay
    }

    // Vẽ lại phần máu chính + màu + chữ
    void RefreshBar(bool instant = false)
    {
        float percent = (maxHealth > 0) ? (float)currentHealth / maxHealth : 0f;

        if (fillImage != null)
        {
            fillImage.fillAmount = percent;
            if (healthGradient != null)
                fillImage.color = healthGradient.Evaluate(percent);
        }

        if (instant && delayedFillImage != null)
            delayedFillImage.fillAmount = percent;

        if (healthText != null)
            healthText.text = currentHealth + " / " + maxHealth;
    }
}
