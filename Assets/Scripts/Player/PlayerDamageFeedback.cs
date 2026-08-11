using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerDamageFeedback : MonoBehaviour
{
    [Header("Optional Existing Overlay")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private Image overlayImage;

    [Header("Damage Pulse")]
    [SerializeField] private Color damageColor = new Color(1f, 0.08f, 0.04f, 1f);
    [SerializeField, Range(0f, 1f)] private float damageMaximumAlpha = 0.32f;
    [SerializeField, Min(0.05f)] private float damageFadeDuration = 0.25f;

    [Header("Dash Avoided Pulse")]
    [SerializeField] private Color avoidedColor = new Color(0.1f, 0.85f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float avoidedMaximumAlpha = 0.16f;
    [SerializeField, Min(0.05f)] private float avoidedFadeDuration = 0.16f;

    private PlayerHealth playerHealth;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();

        if (overlayGroup == null || overlayImage == null)
        {
            CreateRuntimeOverlay();
        }

        overlayGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        playerHealth.Damaged += HandleDamaged;
        playerHealth.DamageAvoided += HandleDamageAvoided;
    }

    private void OnDisable()
    {
        playerHealth.Damaged -= HandleDamaged;
        playerHealth.DamageAvoided -= HandleDamageAvoided;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
        }
    }

    private void HandleDamaged(float damage)
    {
        if (damage > 0f)
        {
            StartPulse(damageColor, damageMaximumAlpha, damageFadeDuration);
        }
    }

    private void HandleDamageAvoided()
    {
        StartPulse(avoidedColor, avoidedMaximumAlpha, avoidedFadeDuration);
    }

    private void StartPulse(Color color, float maximumAlpha, float duration)
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }

        pulseRoutine = StartCoroutine(PulseRoutine(color, maximumAlpha, duration));
    }

    private IEnumerator PulseRoutine(Color color, float maximumAlpha, float duration)
    {
        overlayImage.color = color;
        overlayGroup.alpha = maximumAlpha;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            overlayGroup.alpha = Mathf.Lerp(maximumAlpha, 0f, progress);
            yield return null;
        }

        overlayGroup.alpha = 0f;
        pulseRoutine = null;
    }

    private void CreateRuntimeOverlay()
    {
        GameObject canvasObject = new GameObject(
            "Player Damage Feedback Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        GameObject imageObject = new GameObject(
            "Damage Overlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );

        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        overlayImage = imageObject.GetComponent<Image>();
        overlayImage.color = damageColor;
        overlayImage.raycastTarget = false;

        overlayGroup = imageObject.GetComponent<CanvasGroup>();
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;
    }
}
