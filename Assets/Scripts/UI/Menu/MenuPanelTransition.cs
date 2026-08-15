using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class MenuPanelTransition : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform animatedRect;
    [SerializeField, Min(0.01f)] private float duration = 0.28f;
    [SerializeField] private Vector2 hiddenOffset = new(-36f, 0f);
    [SerializeField, Range(0.8f, 1f)] private float hiddenScale = 0.985f;

    private Vector2 shownPosition;
    private Vector3 shownScale;
    private bool initialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void SetVisibleImmediate(bool visible)
    {
        EnsureInitialized();

        if (visible)
        {
            gameObject.SetActive(true);
        }

        ApplyProgress(visible ? 1f : 0f);
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;

        if (!visible)
        {
            gameObject.SetActive(false);
        }
    }

    public IEnumerator Show()
    {
        EnsureInitialized();
        gameObject.SetActive(true);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        yield return Animate(canvasGroup.alpha, 1f);

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public IEnumerator Hide()
    {
        EnsureInitialized();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        yield return Animate(canvasGroup.alpha, 0f);

        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    private IEnumerator Animate(float from, float to)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / safeDuration);
            float easedTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
            ApplyProgress(Mathf.Lerp(from, to, easedTime));
            yield return null;
        }

        ApplyProgress(to);
    }

    private void ApplyProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        canvasGroup.alpha = progress;
        animatedRect.anchoredPosition = shownPosition + hiddenOffset * (1f - progress);
        animatedRect.localScale = Vector3.Lerp(shownScale * hiddenScale, shownScale, progress);
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        canvasGroup ??= GetComponent<CanvasGroup>();
        animatedRect ??= transform as RectTransform;

        shownPosition = animatedRect.anchoredPosition;
        shownScale = animatedRect.localScale;
        initialized = true;
    }
}
