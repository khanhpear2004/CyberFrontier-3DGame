using UnityEngine;
public class PlayerEnergy : MonoBehaviour
{
    [Header("Chỉ số năng lượng")]
    public float maxEnergy = 200f;
    public float currentEnergy = 200f;

    [Header("Hồi năng lượng (0 = không tự hồi)")]
    public float regenPerSecond = 0f;   // Đặt >0 nếu muốn năng lượng tự đầy lại theo thời gian
    public float regenDelay = 1f;        // Chờ mấy giây sau lần dùng cuối mới bắt đầu hồi

    private float regenTimer = 0f;
    public System.Action<float, float> OnEnergyChanged;
    [SerializeField] private RuntimeStats runtimeStats;

    private void Awake()
    {
        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<RuntimeStats>();
        }
    }

    void Start()
    {
        RefreshStats();
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        Notify();
    }

    private void OnEnable()
    {
        if (runtimeStats != null)
        {
            runtimeStats.StatChanged += HandleStatChanged;
        }
    }

    private void OnDisable()
    {
        if (runtimeStats != null)
        {
            runtimeStats.StatChanged -= HandleStatChanged;
        }
    }

    private void RefreshStats()
    {
        if (runtimeStats == null)
        {
            return;
        }

        maxEnergy = runtimeStats.GetValue(StatType.MaxEnergy);
        regenPerSecond = runtimeStats.GetValue(StatType.EnergyRegen);
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
    }

    private void HandleStatChanged(StatType statType, float oldValue, float newValue)
    {
        if (statType == StatType.MaxEnergy)
        {
            float difference = newValue - oldValue;

            maxEnergy = newValue;

            if (difference > 0f)
            {
                currentEnergy += difference;
            }
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
            Notify();
        }
        else if (statType == StatType.EnergyRegen)
        {
            regenPerSecond = newValue;
        }
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
    public void Add(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
        Notify();
    }

    void Notify() => OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
}
