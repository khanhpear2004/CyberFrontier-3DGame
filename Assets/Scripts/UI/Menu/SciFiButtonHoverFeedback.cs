using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class SciFiButtonHoverFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField] private Image hoverOverlay;
    [SerializeField] private TMP_Text label;
    [SerializeField] private RectTransform animatedRect;
    [SerializeField] private Color hoverColor = new Color32(255, 255, 255, 238);
    [SerializeField] private Color pressedColor = new Color32(150, 18, 32, 245);
    [SerializeField] private Color normalLabelColor = new Color32(213, 250, 255, 255);
    [SerializeField] private Color hoverLabelColor = new Color32(10, 25, 48, 255);
    [SerializeField] private Color pressedLabelColor = Color.white;
    [SerializeField, Min(1f)] private float hoverScale = 1.045f;
    [SerializeField, Min(1f)] private float pressedScale = 1.015f;
    [SerializeField, Min(1f)] private float responseSpeed = 18f;

    private Button button;
    private bool pointerInside;
    private bool selected;
    private bool pressed;
    private Color targetOverlayColor = Color.clear;
    private Color targetLabelColor;
    private Vector3 targetScale = Vector3.one;

    public void Configure(Image overlay, TMP_Text buttonLabel, RectTransform rect)
    {
        hoverOverlay = overlay;
        label = buttonLabel;
        animatedRect = rect;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        animatedRect ??= transform as RectTransform;
        targetLabelColor = normalLabelColor;
        ApplyImmediately();
    }

    private void OnDisable()
    {
        pointerInside = false;
        selected = false;
        pressed = false;
        SetVisualState();
        ApplyImmediately();
    }

    private void Update()
    {
        if (button != null && !button.interactable)
        {
            pointerInside = false;
            pressed = false;
            SetVisualState();
        }

        float blend = 1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime);

        if (hoverOverlay != null)
        {
            hoverOverlay.color = Color.Lerp(hoverOverlay.color, targetOverlayColor, blend);
        }

        if (label != null)
        {
            label.color = Color.Lerp(label.color, targetLabelColor, blend);
        }

        if (animatedRect != null)
        {
            animatedRect.localScale = Vector3.Lerp(animatedRect.localScale, targetScale, blend);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        SetVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        pressed = false;
        SetVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        pressed = true;
        SetVisualState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        SetVisualState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        SetVisualState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        SetVisualState();
    }

    private void SetVisualState()
    {
        bool canInteract = button == null || button.interactable;

        if (!canInteract)
        {
            targetOverlayColor = Color.clear;
            targetLabelColor = normalLabelColor;
            targetScale = Vector3.one;
            return;
        }

        if (pressed)
        {
            targetOverlayColor = pressedColor;
            targetLabelColor = pressedLabelColor;
            targetScale = Vector3.one * pressedScale;
            return;
        }

        if (pointerInside || selected)
        {
            targetOverlayColor = hoverColor;
            targetLabelColor = hoverLabelColor;
            targetScale = Vector3.one * hoverScale;
            return;
        }

        targetOverlayColor = Color.clear;
        targetLabelColor = normalLabelColor;
        targetScale = Vector3.one;
    }

    private void ApplyImmediately()
    {
        if (hoverOverlay != null)
        {
            hoverOverlay.color = targetOverlayColor;
        }

        if (label != null)
        {
            label.color = targetLabelColor;
        }

        if (animatedRect != null)
        {
            animatedRect.localScale = targetScale;
        }
    }
}
