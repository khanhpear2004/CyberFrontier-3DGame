using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[DisallowMultipleComponent]
public class ChestInteractable : MonoBehaviour
{
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private string promptText = "Open Chest";
    [SerializeField] private bool canOpenOnlyOnce = true;

    [Header("VFX")]
    [SerializeField] private ParticleSystem openVfx;

    [Header("Reward Items")]
    [SerializeField] private List<ItemEntry> rewards = new();

    [Header("Disappear")]
    [SerializeField, Min(0f)] private float openAnimationDuration = 4f;
    [SerializeField, Min(0f)] private float disappearDelay = 5f;
    [SerializeField, Min(0f)] private float fadeDuration = 1.5f;
    [SerializeField, Min(0f)] private float rewardPopupDelay = 1.5f;
    [SerializeField] private ChestFader chestFader;

    private Action<ChestInteractable> returnToPool;
    private Coroutine disappearCoroutine;
    private static readonly int OpenTrigger = Animator.StringToHash("Open");
    private bool isOpened;

    public string PromptText => promptText;

    public bool CanInteract => !canOpenOnlyOnce || !isOpened;

    private void Awake()
    {
        if (chestAnimator == null)
        {
            chestAnimator = GetComponentInChildren<Animator>();
        }

        if (chestFader == null)
        {
            chestFader = GetComponent<ChestFader>();
        }

        if (chestFader != null)
        {
            chestFader.ResetFade();
        }

        if (openVfx != null)
        {
            openVfx.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    public void Interact()
    {
        if (!CanInteract || chestAnimator == null)
        {
            return;
        }

        isOpened = true;
        chestAnimator.SetTrigger(OpenTrigger);

        if (openVfx != null)
        {
            openVfx.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            openVfx.Play(true);
        }

        if (disappearCoroutine == null)
        {
            disappearCoroutine = StartCoroutine(DisappearAfterDelay());
        }
    }

    // public void OnOpenAnimationFinished()
    // {
    //     if (!isOpened || disappearCoroutine != null)
    //     {
    //         return;
    //     }

    //     disappearCoroutine = StartCoroutine(DisappearAfterDelay());
    // }

    private IEnumerator DisappearAfterDelay()
    {
        yield return new WaitForSeconds(rewardPopupDelay);

        if (RewardPopupUI.Instance != null && rewards.Count > 0)
        {
            yield return RewardPopupUI.Instance.ShowAndWait(rewards);
        }
        else
        {
            Debug.LogWarning(
                $"{name}: Thiếu RewardPopupUI hoặc RewardItemData."
            );
        }

        yield return new WaitForSeconds(disappearDelay);

        if (openVfx != null)
        {
            openVfx.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (chestFader != null)
        {
            yield return chestFader.FadeOut(fadeDuration);
        }

        disappearCoroutine = null;

        if (returnToPool != null)
        {
            returnToPool.Invoke(this);
        }
        else
        {

            gameObject.SetActive(false);
        }
    }

    public void ResetForReuse()
    {
        if (disappearCoroutine != null)
        {
            StopCoroutine(disappearCoroutine);
            disappearCoroutine = null;
        }

        isOpened = false;

        if (openVfx != null)
        {
            openVfx.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (chestFader != null)
        {
            chestFader.ResetFade();
        }

        if (chestAnimator != null)
        {
            chestAnimator.Rebind();
            chestAnimator.Update(0f);
        }
    }
}