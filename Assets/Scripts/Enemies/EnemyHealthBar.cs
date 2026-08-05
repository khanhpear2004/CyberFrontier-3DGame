using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image delayedFill;

    [Header("Visual")]
    [SerializeField, Min(0f)] private float delayedSpeed = 0.8f;

    private Camera viewCamera;

    private void Awake()
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }

        viewCamera = Camera.main;
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;

        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.HealthChanged += HandleHealthChanged;
        ApplyHealth(enemyHealth.CurrentHealth, enemyHealth.MaximumHealth, true);
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;

        if (enemyHealth != null)
        {
            enemyHealth.HealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        if (healthFill == null || delayedFill == null)
        {
            return;
        }

        if (delayedFill.fillAmount > healthFill.fillAmount)
        {
            delayedFill.fillAmount = Mathf.MoveTowards(delayedFill.fillAmount,healthFill.fillAmount,delayedSpeed * Time.deltaTime);
        }
        else
        {
            delayedFill.fillAmount = healthFill.fillAmount;
        }
    }

    private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        if (renderingCamera == null || renderingCamera.cameraType != CameraType.Game)
        {
            return;
        }

        transform.rotation = renderingCamera.transform.rotation;
    }

    private void HandleHealthChanged(float currentHealth, float maximumHealth)
    {
        ApplyHealth(currentHealth, maximumHealth, false);
    }

    private void ApplyHealth(float currentHealth, float maximumHealth, bool instant)
    {
        float percentage = maximumHealth > 0f ? Mathf.Clamp01(currentHealth / maximumHealth) : 0f;

        if (healthFill != null)
        {
            healthFill.fillAmount = percentage;
        }

        if (delayedFill != null && (instant || percentage > delayedFill.fillAmount))
        {
            delayedFill.fillAmount = percentage;
        }
    }
}