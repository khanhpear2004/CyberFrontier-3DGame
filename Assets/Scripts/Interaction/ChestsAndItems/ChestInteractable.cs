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

    [Header("Lock")]
    [SerializeField] private bool startLocked = true;
    private bool isLocked;

    [Header("VFX")]
    [SerializeField] private ParticleSystem openVfx;

    [Header("Audio")]
    [SerializeField] private AudioCue openCue;

    [Header("Reward Items")]
    [SerializeField] private List<ItemEntry> rewards = new();
    [SerializeField] private RunItemManager runItemManager;

    [Header("Reward Planet Buffs")]
    [SerializeField] private List<PlanetBuffData> planetBuffRewards = new();
    [SerializeField] private RunPlanetBuffManager runPlanetBuffManager;

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
    private bool rewardsGranted;
    public string PromptText => promptText;
    public bool IsLocked => isLocked;
    public bool CanInteract => (!canOpenOnlyOnce || !isOpened) && !isLocked;
    private void Awake()
    {
        isLocked = startLocked;

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

        if (runItemManager == null)
        {
            runItemManager = FindAnyObjectByType<RunItemManager>();
        }

        if (runPlanetBuffManager == null)
        {
            runPlanetBuffManager = FindAnyObjectByType<RunPlanetBuffManager>();
        }

        if (openVfx != null)
        {
            openVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void Lock()
    {
        if (isLocked || isOpened)
        {
            return;
        }

        isLocked = true;
    }

    public void Unlock()
    {
        if (!isLocked || isOpened)
        {
            return;
        }

        isLocked = false;
        Debug.Log($"{name}: Chest đã được mở khóa.", this);
    }

    public void Interact()
    {
        if (!CanInteract || chestAnimator == null)
        {
            return;
        }

        bool rewardsAdded = GrantRewardsToInventory();

        if (!rewardsAdded)
        {
            return;
        }

        isOpened = true;
        chestAnimator.SetTrigger(OpenTrigger);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAt(openCue, transform.position);
        }

        if (openVfx != null)
        {
            openVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            openVfx.Play(true);
        }

        if (disappearCoroutine == null)
        {
            disappearCoroutine = StartCoroutine(DisappearAfterDelay());
        }
    }

    private IEnumerator DisappearAfterDelay()
    {
        yield return new WaitForSeconds(rewardPopupDelay);

        if (RewardPopupUI.Instance != null && rewards.Count > 0)
        {
            yield return RewardPopupUI.Instance.ShowAndWait(rewards, planetBuffRewards);
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
        rewardsGranted = false;
        isLocked = startLocked;

        if (openVfx != null)
        {
            openVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

    private bool GrantRewardsToInventory()
    {
        if (rewardsGranted)
        {
            return true;
        }

        bool hasItemRewards = HasValidItemReward();
        bool hasPlanetBuffRewards = HasValidPlanetBuffReward();

        if (hasItemRewards && runItemManager == null)
        {
            runItemManager = FindAnyObjectByType<RunItemManager>();
        }

        if (hasPlanetBuffRewards && runPlanetBuffManager == null)
        {
            runPlanetBuffManager = FindAnyObjectByType<RunPlanetBuffManager>();
        }

        if (hasItemRewards && runItemManager == null)
        {
            Debug.LogError($"{name}: Không tìm thấy RunItemManager.", this);

            return false;
        }

        if (hasPlanetBuffRewards && runPlanetBuffManager == null)
        {
            Debug.LogError(
                $"{name}: Không tìm thấy RunPlanetBuffManager.",
                this
            );

            return false;
        }

        GrantItemRewards();
        GrantPlanetBuffRewards();

        rewardsGranted = true;

        return true;
    }

    private void GrantItemRewards()
    {
        if (runItemManager == null)
        {
            return;
        }

        foreach (ItemEntry reward in rewards)
        {
            if (reward == null || reward.Item == null)
            {
                continue;
            }

            int amount = Mathf.Max(1, reward.Amount);
            int addedAmount = 0;

            for (int i = 0; i < amount; i++)
            {
                if (runItemManager.AddItem(reward.Item))
                {
                    addedAmount++;
                }
            }

            int totalStack = runItemManager.GetStackCount(reward.Item.ItemId);

            Debug.Log($"Đã thêm {reward.Item.DisplayName} x{addedAmount}. " + $"Tổng số lượng: {totalStack}.", this);
        }
    }

    private void GrantPlanetBuffRewards()
    {
        if (runPlanetBuffManager == null)
        {
            return;
        }

        foreach (PlanetBuffData buffData in planetBuffRewards)
        {
            if (buffData == null)
            {
                continue;
            }

            bool added = runPlanetBuffManager.AddBuff(buffData);

            if (added)
            {
                Debug.Log($"Đã thêm Planet Buff {buffData.DisplayName} vào kho.", this);
            }
            else
            {
                Debug.LogWarning($"{name}: Không thể thêm Planet Buff " + $"{buffData.DisplayName}. Buff có thể đã được sở hữu.", this);
            }
        }
    }

    private bool HasValidItemReward()
    {
        foreach (ItemEntry reward in rewards)
        {
            if (reward != null && reward.Item != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasValidPlanetBuffReward()
    {
        foreach (PlanetBuffData buffData in planetBuffRewards)
        {
            if (buffData != null)
            {
                return true;
            }
        }

        return false;
    }
    
}
