using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private RuntimeStats runtimeStats;

    [Header("Invulnerability")]
    [SerializeField, Min(0f)] private float postHitInvulnerabilityDuration = 0.6f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsDashInvulnerable => dashInvulnerabilityTimer > 0f;
    public bool IsPostHitInvulnerable => postHitInvulnerabilityTimer > 0f;
    public bool IsInvulnerable => IsDashInvulnerable || IsPostHitInvulnerable;

    public event Action<float, float> HealthChanged;
    public event Action<float> Damaged;
    public event Action Died;
    public event Action DamageAvoided;

    private float dashInvulnerabilityTimer;
    private float postHitInvulnerabilityTimer;

    private void Awake()
    {
        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<RuntimeStats>();
        }

        if (GetComponent<PlayerDamageFeedback>() == null)
        {
            gameObject.AddComponent<PlayerDamageFeedback>();
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

    private void Update()
    {
        if (dashInvulnerabilityTimer > 0f)
        {
            dashInvulnerabilityTimer = Mathf.Max(
                0f,
                dashInvulnerabilityTimer - Time.deltaTime
            );
        }

        if (postHitInvulnerabilityTimer > 0f)
        {
            postHitInvulnerabilityTimer = Mathf.Max(
                0f,
                postHitInvulnerabilityTimer - Time.deltaTime
            );
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        if (IsDashInvulnerable)
        {
            DamageAvoided?.Invoke();
            return;
        }

        if (IsPostHitInvulnerable)
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

        postHitInvulnerabilityTimer = postHitInvulnerabilityDuration;

        Damaged?.Invoke(finalDamage);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            Died?.Invoke();
            Debug.Log("Player đã hết máu.", this);
        }
    }

    public void GrantDashInvulnerability(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        dashInvulnerabilityTimer = Mathf.Max(
            dashInvulnerabilityTimer,
            duration
        );
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
