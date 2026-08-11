using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerDeathSequence : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAimShooter playerAimShooter;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private Animator animator;

    [Header("Optional UI")]
    [SerializeField] private InventoryMenuUI inventoryMenuUI;

    [Header("Death VFX")]
    [SerializeField] private GameObject deathVfxPrefab;
    [SerializeField] private Transform deathVfxPoint;
    [SerializeField, Min(0.1f)] private float deathVfxLifetime = 2f;
    [SerializeField, Min(0.1f)] private float deathVfxScale = 1.5f;

    [Header("Sequence")]
    [SerializeField, Min(0.1f)] private float sequenceDuration = 1.4f;
    [SerializeField] private string upperBodyLayerName = "UpperBodyArm";

    public event Action SequenceCompleted;

    public bool IsPlaying { get; private set; }
    public bool HasCompleted { get; private set; }

    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsBeamingHash = Animator.StringToHash("isBeaming");
    private static readonly int IsFlyingHash = Animator.StringToHash("isFlying");
    private static readonly int IsDashingHash = Animator.StringToHash("isDashing");
    private static readonly int AimHorizontalHash = Animator.StringToHash("AimHorizontal");
    private static readonly int AimVerticalHash = Animator.StringToHash("AimVertical");

    private Coroutine sequenceRoutine;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerAimShooter == null)
        {
            playerAimShooter = GetComponent<PlayerAimShooter>();
        }

        if (playerInteractor == null)
        {
            playerInteractor = GetComponent<PlayerInteractor>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void OnEnable()
    {
        playerHealth.Died += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDied;
        }
    }

    private void HandlePlayerDied()
    {
        if (IsPlaying || HasCompleted)
        {
            return;
        }

        sequenceRoutine = StartCoroutine(PlayDeathSequence());
    }

    private IEnumerator PlayDeathSequence()
    {
        IsPlaying = true;

        if (inventoryMenuUI != null)
        {
            if (InventoryMenuUI.IsOpen)
            {
                inventoryMenuUI.Close();
            }

            inventoryMenuUI.enabled = false;
        }

        if (playerAimShooter != null)
        {
            playerAimShooter.CancelAim();
        }

        if (playerController != null)
        {
            playerController.PrepareForDeath();
        }

        if (playerInteractor != null)
        {
            playerInteractor.enabled = false;
        }

        if (playerAimShooter != null)
        {
            playerAimShooter.enabled = false;
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        PrepareAnimator();
        SpawnDeathVfx();

        float elapsed = 0f;

        while (elapsed < sequenceDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        IsPlaying = false;
        HasCompleted = true;
        sequenceRoutine = null;

        SequenceCompleted?.Invoke();
    }

    private void PrepareAnimator()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(SpeedHash, 0f);
        animator.SetFloat(AimHorizontalHash, 0f);
        animator.SetFloat(AimVerticalHash, 0f);

        animator.SetBool(IsBeamingHash, false);
        animator.SetBool(IsFlyingHash, false);
        animator.SetBool(IsDashingHash, false);

        int upperBodyLayerIndex = animator.GetLayerIndex(upperBodyLayerName);

        if (upperBodyLayerIndex >= 0)
        {
            animator.SetLayerWeight(upperBodyLayerIndex, 0f);
        }

        animator.ResetTrigger(DieHash);
        animator.SetTrigger(DieHash);
    }

    private void SpawnDeathVfx()
    {
        if (deathVfxPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = deathVfxPoint != null ? deathVfxPoint.position : transform.position + Vector3.up;

        Quaternion spawnRotation = deathVfxPoint != null ? deathVfxPoint.rotation : Quaternion.identity;

        GameObject instance = Instantiate(
            deathVfxPrefab,
            spawnPosition,
            spawnRotation
        );

        instance.transform.localScale *= deathVfxScale;
        Destroy(instance, deathVfxLifetime);
    }
}