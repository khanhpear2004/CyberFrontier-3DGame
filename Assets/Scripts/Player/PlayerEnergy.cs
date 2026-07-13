using UnityEngine;

// Tài nguyên NĂNG LƯỢNG của người chơi (thay cho cooldown ở các chiêu tốn năng lượng).
// Base 200. Bắn beam tốn 5/lần -> spam được tới khi cạn.
public class PlayerEnergy : MonoBehaviour
{
    [Header("Chỉ số năng lượng")]
    public float maxEnergy = 200f;
    public float currentEnergy = 200f;

    [Header("Hồi năng lượng (0 = không tự hồi)")]
    public float regenPerSecond = 0f;   // Đặt >0 nếu muốn năng lượng tự đầy lại theo thời gian
    public float regenDelay = 1f;        // Chờ mấy giây sau lần dùng cuối mới bắt đầu hồi

    private float regenTimer = 0f;

    // UI đăng ký sự kiện này để cập nhật thanh năng lượng. (current, max)
    public System.Action<float, float> OnEnergyChanged;

    void Start()
    {
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        Notify();
    }

    void Update()
    {
        if (regenPerSecond > 0f && currentEnergy < maxEnergy)
        {
            if (regenTimer > 0f) { regenTimer -= Time.deltaTime; return; }
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + regenPerSecond * Time.deltaTime);
            Notify();
        }
    }

    public bool HasEnough(float amount) => currentEnergy >= amount;

    // Trừ năng lượng nếu đủ. Trả về true nếu trừ thành công.
    public bool TrySpend(float amount)
    {
        if (currentEnergy < amount) return false;
        currentEnergy -= amount;
        regenTimer = regenDelay;   // hoãn hồi sau khi vừa dùng
        Notify();
        return true;
    }

    // Cộng năng lượng (nhặt pin, hồi...).
    public void Add(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
        Notify();
    }

    void Notify() => OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
}
