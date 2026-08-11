using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyPoolMember))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maximumHealth = 100f;

    [Header("Death")]
    [SerializeField, Min(0f)] private float despawnDelay = 2.5f;
    public event Action<float, float> HealthChanged;
    public event Action<float> Damaged;
    public event Action<EnemyHealth> Died;

    public float CurrentHealth => currentHealth;
    public float MaximumHealth => maximumHealth;
    public bool IsDead => isDead;


    private static readonly int DieStateHash = Animator.StringToHash("Base Layer.Die");

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");

    private static readonly int ShootHash = Animator.StringToHash("Shoot");

    private static readonly int DieHash = Animator.StringToHash("Die");

    private EnemyBrain brain;
    private EnemyShooter shooter;
    private EnemyPoolMember poolMember;
    private NavMeshAgent agent;
    private Animator animator;

    private float currentHealth;
    private bool isDead;
    private Coroutine deathRoutine;

    [SerializeField, Min(1f)]
    private float deathAnimationTimeout = 5f;

    private EnemySpawnTransition spawnTransition;

    private void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        shooter = GetComponent<EnemyShooter>();
        poolMember = GetComponent<EnemyPoolMember>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        spawnTransition = GetComponent<EnemySpawnTransition>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        float appliedDamage = previousHealth - currentHealth;

        if (appliedDamage > 0f)
        {
            Damaged?.Invoke(appliedDamage);
        }

        HealthChanged?.Invoke(currentHealth, maximumHealth);

        if (currentHealth <= 0f)
        {
            BeginDeath();
        }
    }

    public void ResetForSpawn()
    {
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        isDead = false;
        currentHealth = maximumHealth;
        HealthChanged?.Invoke(currentHealth, maximumHealth);

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);

            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsAimingHash, false);
            animator.ResetTrigger(ShootHash);
            animator.ResetTrigger(DieHash);
        }

        if (agent != null)
        {
            agent.enabled = true;
        }

        if (brain != null)
        {
            brain.enabled = true;
        }

        if (shooter != null)
        {
            shooter.enabled = true;
        }
    }

    private void BeginDeath()
    {
        isDead = true;

        shooter?.StopForPool();

        if (shooter != null)
        {
            shooter.enabled = false;
        }

        if (brain != null)
        {
            brain.enabled = false;
        }

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsAimingHash, false);
            animator.ResetTrigger(ShootHash);
            animator.SetTrigger(DieHash);
        }

        spawnTransition?.HideHealthBar();
        Died?.Invoke(this);
        deathRoutine = StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        float elapsed = 0f;
        const int layerIndex = 0;

        while (animator != null && elapsed < deathAnimationTimeout)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layerIndex);

            bool isPlayingDie = state.fullPathHash == DieStateHash;

            bool hasFinished = isPlayingDie && state.normalizedTime >= 1f && !animator.IsInTransition(layerIndex);

            if (hasFinished) break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        deathRoutine = null;

        if (spawnTransition != null)
        {
            spawnTransition.PlayDisappear(poolMember.Despawn);
        }
        else
        {
            poolMember.Despawn();
        }
    }
}
