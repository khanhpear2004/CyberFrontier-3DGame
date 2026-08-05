using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlanetBuffLoadoutSlotView : MonoBehaviour
{
    [Header("States")]
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject buffContent;

    [Header("Buff Content")]
    [SerializeField] private Image buffIcon;

    [Header("Interaction")]
    [SerializeField] private Button button;

    private PlanetBuffData boundBuff;
    private Action<PlanetBuffData> clickedCallback;

    private void Awake()
    {
        ValidateReferences();

        if (button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }

        SetEmpty();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    public void SetEmpty()
    {
        boundBuff = null;
        clickedCallback = null;

        if (emptyState != null)
        {
            emptyState.SetActive(true);
        }

        if (buffContent != null)
        {
            buffContent.SetActive(false);
        }

        if (buffIcon != null)
        {
            buffIcon.sprite = null;
            buffIcon.enabled = false;
        }

        if (button != null)
        {
            button.interactable = false;
        }
    }

    public void Bind(PlanetBuffData buffData, Action<PlanetBuffData> onClicked)
    {
        if (buffData == null)
        {
            SetEmpty();
            return;
        }

        boundBuff = buffData;
        clickedCallback = onClicked;

        if (emptyState != null)
        {
            emptyState.SetActive(false);
        }

        if (buffContent != null)
        {
            buffContent.SetActive(true);
        }

        if (buffIcon != null)
        {
            buffIcon.sprite = buffData.Icon;
            buffIcon.enabled = buffData.Icon != null;
        }

        if (button != null)
        {
            button.interactable = true;
        }
    }

    private void HandleClicked()
    {
        if (boundBuff == null)
        {
            return;
        }

        clickedCallback?.Invoke(boundBuff);
    }

    private void ValidateReferences()
    {
        if (emptyState == null)
        {
            Debug.LogError($"{name}: EmptyState has not been assigned.", this);
        }

        if (buffContent == null)
        {
            Debug.LogError($"{name}: BuffContent has not been assigned.", this);
        }

        if (buffIcon == null)
        {
            Debug.LogError($"{name}: BuffIcon has not been assigned.", this);
        }

        if (button == null)
        {
            Debug.LogError($"{name}: Button has not been assigned.", this);
        }
    }
}
