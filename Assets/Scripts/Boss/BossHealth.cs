using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maximumHealth = 800f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider[] damageColliders = Array.Empty<Collider>();

    [Header("Death")]
    [SerializeField] private string deathTriggerName = "Die";
    [SerializeField] private string deathStateName = "A_MechISO_Death 0";
    [SerializeField, Min(0f)] private float deathAnimationDuration = 5f;

    [Header("Landing")]
    [SerializeField] private string landingStateName = "A_MechISO_Landing";

    private float currentHealth;
    private bool isActive;
    private bool isDead;
    private Coroutine deathAnimationRoutine;

    public float CurrentHealth => currentHealth;
    public float MaximumHealth => maximumHealth;
    public float HealthNormalized => maximumHealth > 0f ? currentHealth / maximumHealth : 0f;
    public bool IsActive => isActive;
    public bool IsDead => isDead;

    public event Action<float, float> HealthChanged;
    public event Action<float> Damaged;
    public event Action<BossHealth> BossActivated;
    public event Action<BossHealth> Died;
    public event Action<BossHealth> DeathAnimationCompleted;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (damageColliders == null || damageColliders.Length == 0)
        {
            damageColliders = GetComponentsInChildren<Collider>(true);
        }

        if (!HasRequiredReferences())
        {
            enabled = false;
            return;
        }

        SetDormantState();
    }

    private void OnEnable()
    {
        if (enabled)
        {
            SetDormantState();
        }
    }

    private void OnDisable()
    {
        if (deathAnimationRoutine != null)
        {
            StopCoroutine(deathAnimationRoutine);
            deathAnimationRoutine = null;
        }
    }

    public void ActivateBoss()
    {
        if (!enabled)
        {
            return;
        }

        if (deathAnimationRoutine != null)
        {
            StopCoroutine(deathAnimationRoutine);
            deathAnimationRoutine = null;
        }

        currentHealth = maximumHealth;
        isActive = true;
        isDead = false;
        SetDamageCollidersEnabled(true);
        ResetAnimator();
        HealthChanged?.Invoke(currentHealth, maximumHealth);
        BossActivated?.Invoke(this);
    }

    public void TakeDamage(float amount)
    {
        if (!isActive || isDead || amount <= 0f)
        {
            return;
        }

        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        float appliedDamage = previousHealth - currentHealth;

        if (appliedDamage <= 0f)
        {
            return;
        }

        Damaged?.Invoke(appliedDamage);
        HealthChanged?.Invoke(currentHealth, maximumHealth);

        if (currentHealth <= 0f)
        {
            BeginDeath();
        }
    }

    public void SetDormantState()
    {
        if (deathAnimationRoutine != null)
        {
            StopCoroutine(deathAnimationRoutine);
            deathAnimationRoutine = null;
        }

        currentHealth = maximumHealth;
        isActive = false;
        isDead = false;
        SetDamageCollidersEnabled(false);
        ResetAnimator();
        HealthChanged?.Invoke(currentHealth, maximumHealth);
    }

    public void PlayLandingAnimation(float normalizedStartTime)
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(landingStateName))
        {
            return;
        }

        int landingStateHash = Animator.StringToHash($"Base Layer.{landingStateName}");

        if (animator.HasState(0, landingStateHash))
        {
            animator.Play(landingStateHash, 0, Mathf.Clamp01(normalizedStartTime));
            animator.Update(0f);
        }
    }

    [ContextMenu("Debug/Activate Boss")]
    public void DebugActivateBoss()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ActivateBoss();
#endif
    }

    [ContextMenu("Debug/Kill Boss")]
    public void DebugKillBoss()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!isActive)
        {
            ActivateBoss();
        }

        TakeDamage(maximumHealth);
#endif
    }

    private void BeginDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isActive = false;
        SetDamageCollidersEnabled(false);
        PlayDeathAnimation();
        Died?.Invoke(this);

        if (deathAnimationDuration > 0f)
        {
            deathAnimationRoutine = StartCoroutine(WaitForDeathAnimation());
        }
        else
        {
            DeathAnimationCompleted?.Invoke(this);
        }
    }

    private IEnumerator WaitForDeathAnimation()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        deathAnimationRoutine = null;
        DeathAnimationCompleted?.Invoke(this);
    }

    private void ResetAnimator()
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            return;
        }

        animator.Rebind();
        animator.Update(0f);

        if (!string.IsNullOrWhiteSpace(deathTriggerName) && HasAnimatorTrigger(deathTriggerName))
        {
            animator.ResetTrigger(deathTriggerName);
        }
    }

    private void PlayDeathAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(deathTriggerName) && HasAnimatorTrigger(deathTriggerName))
        {
            animator.SetTrigger(deathTriggerName);
            return;
        }

        int deathStateHash = Animator.StringToHash($"Base Layer.{deathStateName}");

        if (animator.HasState(0, deathStateHash))
        {
            animator.CrossFade(deathStateHash, 0.1f, 0, 0f);
        }
    }

    private void SetDamageCollidersEnabled(bool shouldEnable)
    {
        foreach (Collider damageCollider in damageColliders)
        {
            if (damageCollider != null)
            {
                damageCollider.enabled = shouldEnable;
            }
        }
    }

    private bool HasAnimatorTrigger(string triggerName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasRequiredReferences()
    {
        if (animator == null)
        {
            Debug.LogError($"{name}: Missing Animator.", this);
            return false;
        }

        if (damageColliders == null || damageColliders.Length == 0)
        {
            Debug.LogError($"{name}: Missing Damage Colliders.", this);
            return false;
        }

        return true;
    }
}
