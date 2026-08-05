using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private RuntimeStats runtimeStats;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<float, float> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<RuntimeStats>();
        }

        CurrentHealth = maxHealth;
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

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        float damageMultiplier = 1f;

        if (runtimeStats != null)
        {
            damageMultiplier = runtimeStats.GetValue(StatType.DamageTaken);
        }

        float finalDamage = amount * Mathf.Max(0f, damageMultiplier);

        if (finalDamage <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Max(CurrentHealth - finalDamage, 0f);

        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            Died?.Invoke();
            Debug.Log("Player đã hết máu.", this);
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);

        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void HandleStatChanged(StatType statType, float oldValue, float newValue)
    {
        if (statType != StatType.MaxHealth)
        {
            return;
        }

        float previousMaxHealth = maxHealth;

        maxHealth = Mathf.Max(1f, newValue);

        float difference = maxHealth - previousMaxHealth;

        if (!IsDead && difference > 0f)
        {
            CurrentHealth += difference;
        }

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}