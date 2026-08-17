using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BossHealthBarUI : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image delayedFill;
    [SerializeField] private TMP_Text healthText;

    [Header("Display")]
    [SerializeField, Min(0f)] private float hideDelayAfterDeath = 1.5f;
    [SerializeField, Min(0.01f)] private float damageTrailSpeed = 0.35f;

    private BossHealth bossHealth;
    private Coroutine hideRoutine;

    private void Awake()
    {
        SetVisible(false);
    }

    private void Update()
    {
        UpdateDamageTrail();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void Initialize(BossHealth health)
    {
        Unsubscribe();
        bossHealth = health;
        Subscribe();
        ApplyHealth(bossHealth != null ? bossHealth.CurrentHealth : 0f, bossHealth != null ? bossHealth.MaximumHealth : 1f, true);
        SetVisible(bossHealth != null && bossHealth.IsActive && !bossHealth.IsDead);
    }

    private void Subscribe()
    {
        if (bossHealth == null)
        {
            return;
        }

        bossHealth.HealthChanged += HandleHealthChanged;
        bossHealth.BossActivated += HandleBossActivated;
        bossHealth.Died += HandleBossDied;
    }

    private void Unsubscribe()
    {
        if (bossHealth == null)
        {
            return;
        }

        bossHealth.HealthChanged -= HandleHealthChanged;
        bossHealth.BossActivated -= HandleBossActivated;
        bossHealth.Died -= HandleBossDied;
    }

    private void HandleHealthChanged(float currentHealth, float maximumHealth)
    {
        ApplyHealth(currentHealth, maximumHealth, false);
    }

    private void HandleBossActivated(BossHealth activatedBoss)
    {
        if (activatedBoss != bossHealth)
        {
            return;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        ApplyHealth(bossHealth.CurrentHealth, bossHealth.MaximumHealth, true);
        SetVisible(true);
    }

    private void HandleBossDied(BossHealth defeatedBoss)
    {
        if (defeatedBoss != bossHealth)
        {
            return;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfterDeathRoutine());
    }

    private IEnumerator HideAfterDeathRoutine()
    {
        if (hideDelayAfterDeath > 0f)
        {
            yield return new WaitForSeconds(hideDelayAfterDeath);
        }

        SetVisible(false);
        hideRoutine = null;
    }

    private void ApplyHealth(float currentHealth, float maximumHealth, bool instant)
    {
        float normalizedHealth = maximumHealth > 0f ? Mathf.Clamp01(currentHealth / maximumHealth) : 0f;

        if (healthFill != null)
        {
            healthFill.fillAmount = normalizedHealth;
        }

        if (delayedFill != null && (instant || delayedFill.fillAmount < normalizedHealth))
        {
            delayedFill.fillAmount = normalizedHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maximumHealth)}";
        }
    }

    private void UpdateDamageTrail()
    {
        if (healthFill == null || delayedFill == null || delayedFill.fillAmount <= healthFill.fillAmount)
        {
            return;
        }

        delayedFill.fillAmount = Mathf.MoveTowards(delayedFill.fillAmount, healthFill.fillAmount, damageTrailSpeed * Time.deltaTime);
    }

    private void SetVisible(bool shouldShow)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = shouldShow ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
