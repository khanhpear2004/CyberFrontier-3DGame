using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class RewardPopupUI : MonoBehaviour
{
    public static RewardPopupUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private RectTransform animatedPanel;
    [SerializeField] private RectTransform itemLine;
    [SerializeField] private RewardItemView itemViewPrefab;
    private readonly List<RewardItemView> itemViewPool = new();

    [Header("Transition")]
    [SerializeField, Min(0f)] private float openDuration = 0.25f;
    [SerializeField, Min(0f)] private float closeDuration = 0.18f;
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.9f, 0.9f, 1f);

    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private bool closeRequested;

    public static bool IsGameplayInputBlocked { get; private set; }

    [SerializeField] private InputActionAsset inputActions;

    private InputActionMap playerActionMap;

    private bool wasPlayerMapEnabled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (popupCanvasGroup == null && popupRoot != null)
        {
            popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();
        }

        if (popupRoot != null)
        {
            if (popupCanvasGroup != null)
            {
                popupCanvasGroup.alpha = 0f;
                popupCanvasGroup.interactable = false;
                popupCanvasGroup.blocksRaycasts = false;
            }

            popupRoot.SetActive(false);
        }

        playerActionMap = inputActions.FindActionMap("Player", false);
    }

    public IEnumerator ShowAndWait(IReadOnlyList<ItemEntry> rewards)
    {
        if (popupRoot == null || popupCanvasGroup == null || animatedPanel == null || itemLine == null)
        {
            Debug.LogWarning("RewardPopupUI đang thiếu reference.");
            yield break;
        }

        closeRequested = false;

        PopulateRewards(rewards);

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        popupRoot.SetActive(true);

        popupCanvasGroup.alpha = 0f;
        popupCanvasGroup.interactable = false;
        popupCanvasGroup.blocksRaycasts = true;

        animatedPanel.localScale = hiddenScale;

        yield return AnimatePopup(0f, 1f, hiddenScale, Vector3.one, openDuration);

        popupCanvasGroup.interactable = true;

        IsGameplayInputBlocked = true;

        if (playerActionMap != null)
        {
            wasPlayerMapEnabled = playerActionMap.enabled;
            playerActionMap.Disable();
        }

        while (!closeRequested)
        {
            yield return null;
        }

        popupCanvasGroup.interactable = false;

        yield return AnimatePopup(popupCanvasGroup.alpha, 0f, animatedPanel.localScale, hiddenScale, closeDuration);

        popupCanvasGroup.blocksRaycasts = false;
        popupRoot.SetActive(false);

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;

        if (playerActionMap != null && wasPlayerMapEnabled)
        {
            playerActionMap.Enable();
        }

        IsGameplayInputBlocked = false;
    }

    private void PopulateRewards(IReadOnlyList<ItemEntry> rewards)
    {
        int visibleIndex = 0;

        for (int i = 0; i < rewards.Count; i++)
        {
            ItemEntry reward = rewards[i];

            if (reward == null || reward.Item == null)
            {
                continue;
            }

            RewardItemView view = GetOrCreateView(visibleIndex);

            view.Bind(reward.Item, reward.Amount);
            view.gameObject.SetActive(true);

            visibleIndex++;
        }

        for (int i = visibleIndex; i < itemViewPool.Count; i++)
        {
            itemViewPool[i].gameObject.SetActive(false);
        }
    }

    private RewardItemView GetOrCreateView(int index)
    {
        while (itemViewPool.Count <= index)
        {
            RewardItemView newView = Instantiate(
                itemViewPrefab,
                itemLine
            );

            itemViewPool.Add(newView);
        }

        return itemViewPool[index];
    }

    public void RequestClose()
    {
        closeRequested = true;
    }

    private IEnumerator AnimatePopup(float startAlpha, float endAlpha, Vector3 startScale, Vector3 endScale, float duration)
    {
        if (duration <= 0f)
        {
            popupCanvasGroup.alpha = endAlpha;
            animatedPanel.localScale = endScale;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            popupCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);

            animatedPanel.localScale = Vector3.Lerp(startScale, endScale, easedProgress);

            yield return null;
        }

        popupCanvasGroup.alpha = endAlpha;
        animatedPanel.localScale = endScale;
    }
}