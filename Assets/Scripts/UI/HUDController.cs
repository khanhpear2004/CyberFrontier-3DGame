using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Tham chiếu thanh máu")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Image delayedFillImage;
    [SerializeField] private TMP_Text healthText;

    [Header("Hiệu ứng")]
    [SerializeField] private Gradient healthGradient;
    [SerializeField, Min(0f)] private float trailSpeed = 0.5f;


    [Header("Phím test")]
    [SerializeField] private bool enableTestKeys = true;
    [SerializeField] private KeyCode damageKey = KeyCode.K;
    [SerializeField] private KeyCode healKey = KeyCode.L;
    [SerializeField, Min(1)] private int testAmount = 10;

    void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogError("HUDController không tìm thấy PlayerHealth.", this);
            return;
        }

        playerHealth.HealthChanged += HandleHealthChanged;

        ApplyHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth, true);
    }

    private void Update()
    {
        HandleTestInput();
        UpdateDelayedHealthBar();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleTestInput()
    {
        if (!enableTestKeys || playerHealth == null)
        {
            return;
        }

        if (Input.GetKeyDown(damageKey))
        {
            playerHealth.TakeDamage(testAmount);
        }

        if (Input.GetKeyDown(healKey))
        {
            playerHealth.Heal(testAmount);
        }
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        ApplyHealth(currentHealth, maxHealth, false);
    }

    private void ApplyHealth(float currentHealth,float maxHealth,bool instant)
    {
        float percent = maxHealth > 0f ? currentHealth / maxHealth : 0f;

        if (fillImage != null)
        {
            fillImage.fillAmount = percent;

            if (healthGradient != null)
            {
                fillImage.color = healthGradient.Evaluate(percent);
            }
        }

        if (instant && delayedFillImage != null)
        {
            delayedFillImage.fillAmount = percent;
        }

        if (healthText != null)
        {
            healthText.text =$"{Mathf.CeilToInt(currentHealth)} / " + $"{Mathf.CeilToInt(maxHealth)}";
        }
    }

    private void UpdateDelayedHealthBar()
    {
        if (fillImage == null || delayedFillImage == null)
        {
            return;
        }

        if (delayedFillImage.fillAmount > fillImage.fillAmount)
        {
            delayedFillImage.fillAmount = Mathf.MoveTowards(delayedFillImage.fillAmount, fillImage.fillAmount, trailSpeed * Time.deltaTime);
        }
        else
        {
            delayedFillImage.fillAmount = fillImage.fillAmount;
        }
    }
}
