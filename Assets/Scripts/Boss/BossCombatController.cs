using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class BossCombatController : MonoBehaviour
{
    private enum MotionState
    {
        None,
        Idle,
        WalkForward,
        WalkBackward
    }

    [Header("References")]
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private Transform[] projectileOrigins = Array.Empty<Transform>();
    [SerializeField] private Transform groundCenter;
    [SerializeField] private EnemyProjectilePool projectilePool;
    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float movementSpeed = 2.5f;
    [SerializeField, Min(0.1f)] private float acceleration = 8f;
    [SerializeField, Min(0f)] private float minimumDistance = 5f;
    [SerializeField, Min(0.1f)] private float preferredDistance = 8f;
    [SerializeField, Min(1f)] private float rotationSpeed = 120f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.25f;
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 4f;

    [Header("Cannon Burst")]
    [SerializeField, Min(0.1f)] private float maximumAttackDistance = 14f;
    [SerializeField, Range(1f, 90f)] private float requiredFacingAngle = 10f;
    [SerializeField, Range(0.7f, 1f)] private float telegraphDuration = 0.8f;
    [SerializeField, Min(1)] private int burstShotCount = 3;
    [SerializeField, Min(0.05f)] private float burstShotInterval = 0.22f;
    [SerializeField, Min(0f)] private float recoveryDuration = 1.5f;
    [SerializeField, Min(0f)] private float attackCooldown = 1f;
    [SerializeField, Min(0f)] private float initialAttackDelay = 1f;
    [SerializeField, Min(0f)] private float muzzleSeparation = 0.8f;
    [SerializeField] private Vector3 muzzleLocalOffset = new Vector3(0f, 0f, 1.8f);
    [SerializeField, Min(0f)] private float targetHeightOffset = 1f;

    [Header("Ground Slam")]
    [SerializeField, Min(1)] private int normalAttackWavesBeforeSlam = 3;
    [SerializeField, Min(0.1f)] private float slamTriggerDistance = 4f;
    [SerializeField, Min(0.1f)] private float slamDamageRadius = 4.5f;
    [SerializeField, Min(0f)] private float slamDamage = 25f;
    [SerializeField, Min(0.1f)] private float slamTelegraphDuration = 1f;
    [SerializeField, Min(0f)] private float slamRecoveryDuration = 2f;
    [SerializeField, Min(0f)] private float slamCooldown = 6f;
    [SerializeField, Min(0f)] private float slamInitialDelay = 2f;
    [SerializeField, Min(0f)] private float slamVerticalTolerance = 2.5f;
    [SerializeField, Range(12, 128)] private int slamCircleSegments = 64;
    [SerializeField] private string slamAnimationStateName = "A_MechISO_Landing";
    [SerializeField, Range(0f, 0.95f)] private float slamAnimationStartNormalizedTime = 0.3f;

    [Header("Animation States")]
    [SerializeField] private string idleStateName = "A_MechISO_Idle";
    [SerializeField] private string walkForwardStateName = "A_MechISO_WalkForward";
    [SerializeField] private string walkBackwardStateName = "A_MechISO_WalkBackward";
    [SerializeField, Min(0f)] private float animationBlendDuration = 0.15f;
    [SerializeField, Min(0.1f)] private float walkAnimationSpeed = 1.15f;

    private NavMeshAgent agent;
    private LineRenderer telegraphLine;
    private LineRenderer slamTelegraphRing;
    private Material telegraphMaterial;
    private Coroutine attackRoutine;
    private MotionState motionState;
    private float nextRepathTime;
    private float nextAttackTime;
    private float nextSlamTime;
    private int completedNormalAttackWaves;
    private bool combatActive;
    private bool hasNavMeshPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (bossHealth == null)
        {
            bossHealth = GetComponent<BossHealth>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (projectileOrigin == null)
        {
            projectileOrigin = transform.Find("ProjectileOrigin");
        }

        if (groundCenter == null)
        {
            groundCenter = transform.Find("GroundCenter");
        }

        ResolveProjectileOrigins();

        ConfigureAgent();
        CreateTelegraphLine();
        CreateSlamTelegraphRing();
    }

    private void OnEnable()
    {
        if (bossHealth == null)
        {
            return;
        }

        bossHealth.BossActivated += HandleBossActivated;
        bossHealth.Died += HandleBossDied;

        if (bossHealth.IsActive)
        {
            BeginCombat();
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.BossActivated -= HandleBossActivated;
            bossHealth.Died -= HandleBossDied;
        }

        StopCombat();
    }

    private void OnDestroy()
    {
        if (telegraphMaterial != null)
        {
            Destroy(telegraphMaterial);
        }
    }

    private void Update()
    {
        if (!combatActive || bossHealth == null || !bossHealth.IsActive || bossHealth.IsDead)
        {
            return;
        }

        ResolveRuntimeReferences();

        if (target == null)
        {
            StopMoving();
            return;
        }

        Vector3 planarDirection = GetPlanarDirectionToTarget();
        float distance = planarDirection.magnitude;

        RotateTowards(planarDirection);

        if (attackRoutine != null)
        {
            StopAgentMovement();
            return;
        }

        if (IsGroundSlamQueued())
        {
            UpdateGroundSlamApproach(distance);

            if (CanGroundSlam(distance))
            {
                attackRoutine = StartCoroutine(GroundSlamRoutine());
            }

            return;
        }

        UpdateMovement(planarDirection, distance);

        if (CanAttack(planarDirection, distance))
        {
            attackRoutine = StartCoroutine(FireBurstRoutine());
        }
    }

    private void HandleBossActivated(BossHealth activatedBoss)
    {
        if (activatedBoss == bossHealth)
        {
            BeginCombat();
        }
    }

    private void HandleBossDied(BossHealth defeatedBoss)
    {
        if (defeatedBoss == bossHealth)
        {
            StopCombat();
        }
    }

    private void BeginCombat()
    {
        ResolveRuntimeReferences();
        combatActive = true;
        nextAttackTime = Time.time + initialAttackDelay;
        nextSlamTime = Time.time + slamInitialDelay;
        completedNormalAttackWaves = 0;
        motionState = MotionState.None;
        hasNavMeshPosition = TryEnableAgentOnNavMesh();
        SetMotionState(MotionState.Idle);
    }

    private void StopCombat()
    {
        combatActive = false;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        SetTelegraphVisible(false);
        SetSlamTelegraphVisible(false);
        StopMoving();

        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        hasNavMeshPosition = false;
        motionState = MotionState.None;

        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    private void ResolveRuntimeReferences()
    {
        if (target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            target = playerObject != null ? playerObject.transform : null;
        }

        if (projectilePool == null)
        {
            projectilePool = FindAnyObjectByType<EnemyProjectilePool>();
        }
    }

    private void ConfigureAgent()
    {
        agent.speed = movementSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = rotationSpeed;
        agent.stoppingDistance = preferredDistance;
        agent.updateRotation = false;
        agent.autoBraking = true;
        agent.enabled = false;
    }

    private bool TryEnableAgentOnNavMesh()
    {
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: Boss is not close enough to a baked NavMesh. It will rotate and shoot, but cannot move.", this);
            return false;
        }

        transform.position = hit.position;
        agent.enabled = true;
        agent.Warp(hit.position);
        return agent.isOnNavMesh;
    }

    private void UpdateMovement(Vector3 planarDirection, float distance)
    {
        if (!hasNavMeshPosition || !agent.enabled || !agent.isOnNavMesh)
        {
            SetMotionState(MotionState.Idle);
            return;
        }

        if (Time.time < nextRepathTime)
        {
            UpdateMotionFromVelocity();
            return;
        }

        nextRepathTime = Time.time + repathInterval;
        agent.stoppingDistance = preferredDistance;

        if (distance > preferredDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
            SetMotionState(MotionState.WalkForward);
            return;
        }

        if (distance < minimumDistance && planarDirection.sqrMagnitude > 0.001f)
        {
            Vector3 retreatPosition = transform.position - planarDirection.normalized * (preferredDistance - distance);

            if (NavMesh.SamplePosition(retreatPosition, out NavMeshHit retreatHit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(retreatHit.position);
                SetMotionState(MotionState.WalkBackward);
                return;
            }
        }

        StopMoving();
    }

    private void UpdateMotionFromVelocity()
    {
        if (agent.velocity.sqrMagnitude >= 0.01f)
        {
            float forwardVelocity = Vector3.Dot(transform.forward, agent.velocity.normalized);
            SetMotionState(forwardVelocity >= -0.1f ? MotionState.WalkForward : MotionState.WalkBackward);
            return;
        }

        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            SetMotionState(MotionState.Idle);
        }
    }

    private void StopMoving()
    {
        StopAgentMovement();

        if (combatActive)
        {
            SetMotionState(MotionState.Idle);
        }
    }

    private void StopAgentMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private Vector3 GetPlanarDirectionToTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        return direction;
    }

    private void RotateTowards(Vector3 planarDirection)
    {
        if (planarDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
    }

    private bool CanAttack(Vector3 planarDirection, float distance)
    {
        if (projectilePool == null || GetProjectileOrigin(0) == null || distance > maximumAttackDistance || Time.time < nextAttackTime)
        {
            return false;
        }

        float facingAngle = Vector3.Angle(transform.forward, planarDirection);
        return facingAngle <= requiredFacingAngle;
    }

    private bool CanGroundSlam(float distance)
    {
        return IsGroundSlamQueued() && distance <= slamTriggerDistance && Time.time >= nextSlamTime;
    }

    private bool IsGroundSlamQueued()
    {
        return completedNormalAttackWaves >= normalAttackWavesBeforeSlam;
    }

    private void UpdateGroundSlamApproach(float distance)
    {
        if (distance <= slamTriggerDistance)
        {
            StopMoving();
            return;
        }

        if (!hasNavMeshPosition || !agent.enabled || !agent.isOnNavMesh)
        {
            SetMotionState(MotionState.Idle);
            return;
        }

        if (Time.time < nextRepathTime)
        {
            return;
        }

        nextRepathTime = Time.time + repathInterval;
        agent.stoppingDistance = Mathf.Max(0.5f, slamTriggerDistance * 0.75f);
        agent.isStopped = false;
        agent.SetDestination(target.position);
        SetMotionState(MotionState.WalkForward);
    }

    private IEnumerator GroundSlamRoutine()
    {
        StopMoving();
        PlayGroundSlamAnimation();
        float elapsed = 0f;
        SetSlamTelegraphVisible(true);

        while (elapsed < slamTelegraphDuration && combatActive && bossHealth.IsActive && target != null)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / slamTelegraphDuration);
            RotateTowards(GetPlanarDirectionToTarget());
            UpdateSlamTelegraph(progress, Mathf.Lerp(slamDamageRadius * 0.2f, slamDamageRadius, progress));
            yield return null;
        }

        if (combatActive && bossHealth.IsActive)
        {
            ApplyGroundSlamDamage();
            UpdateSlamTelegraph(1f, slamDamageRadius * 1.12f);
            yield return new WaitForSeconds(0.12f);
        }

        SetSlamTelegraphVisible(false);

        if (slamRecoveryDuration > 0f && combatActive && bossHealth.IsActive)
        {
            yield return new WaitForSeconds(slamRecoveryDuration);
        }

        nextSlamTime = Time.time + slamCooldown;
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.75f);
        completedNormalAttackWaves = 0;
        agent.stoppingDistance = preferredDistance;
        motionState = MotionState.None;
        SetMotionState(MotionState.Idle);
        attackRoutine = null;
    }

    private void PlayGroundSlamAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(slamAnimationStateName))
        {
            return;
        }

        int slamStateHash = Animator.StringToHash($"Base Layer.{slamAnimationStateName}");

        if (animator.HasState(0, slamStateHash))
        {
            animator.speed = 1f;
            animator.Play(slamStateHash, 0, slamAnimationStartNormalizedTime);
            animator.Update(0f);
            Debug.Log($"{name}: Ground Slam animation started at {slamAnimationStartNormalizedTime:0.00}.", this);
        }
    }

    private void ApplyGroundSlamDamage()
    {
        if (target == null)
        {
            return;
        }

        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
        {
            return;
        }

        Vector3 slamCenter = GetSlamCenter();
        Vector3 playerOffset = playerHealth.transform.position - slamCenter;
        float verticalDistance = Mathf.Abs(playerOffset.y);
        playerOffset.y = 0f;

        if (playerOffset.sqrMagnitude <= slamDamageRadius * slamDamageRadius && verticalDistance <= slamVerticalTolerance)
        {
            playerHealth.TakeDamage(slamDamage);
        }
    }

    private IEnumerator FireBurstRoutine()
    {
        StopMoving();
        float elapsed = 0f;
        int firedShotCount = 0;
        SetTelegraphVisible(true);

        while (elapsed < telegraphDuration && combatActive && bossHealth.IsActive && target != null)
        {
            elapsed += Time.deltaTime;
            Vector3 planarDirection = GetPlanarDirectionToTarget();
            RotateTowards(planarDirection);
            UpdateTelegraphLine(Mathf.Clamp01(elapsed / telegraphDuration));
            yield return null;
        }

        SetTelegraphVisible(false);

        for (int shotIndex = 0; shotIndex < burstShotCount && combatActive && bossHealth.IsActive && target != null; shotIndex++)
        {
            Transform muzzle = GetProjectileOrigin(shotIndex);
            Vector3 spawnPosition = GetMuzzlePosition(muzzle, shotIndex);
            Vector3 targetPosition = target.position + Vector3.up * targetHeightOffset;
            Vector3 shotDirection = (targetPosition - spawnPosition).normalized;
            projectilePool.Spawn(transform, spawnPosition, shotDirection);
            firedShotCount++;

            if (shotIndex < burstShotCount - 1)
            {
                yield return new WaitForSeconds(burstShotInterval);
            }
        }

        if (firedShotCount > 0)
        {
            completedNormalAttackWaves++;
        }

        if (recoveryDuration > 0f)
        {
            yield return new WaitForSeconds(recoveryDuration);
        }

        nextAttackTime = Time.time + attackCooldown;
        attackRoutine = null;
    }

    private void SetMotionState(MotionState newState)
    {
        if (motionState == newState || animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            return;
        }

        motionState = newState;
        animator.speed = newState == MotionState.WalkForward || newState == MotionState.WalkBackward ? walkAnimationSpeed : 1f;
        string stateName = newState == MotionState.WalkForward ? walkForwardStateName : newState == MotionState.WalkBackward ? walkBackwardStateName : idleStateName;
        int stateHash = Animator.StringToHash($"Base Layer.{stateName}");

        if (animator.HasState(0, stateHash))
        {
            animator.CrossFade(stateHash, animationBlendDuration, 0, 0f);
        }
    }

    private void CreateTelegraphLine()
    {
        GameObject lineObject = new GameObject("BossCannonTelegraph");
        lineObject.transform.SetParent(transform, false);
        telegraphLine = lineObject.AddComponent<LineRenderer>();
        telegraphLine.useWorldSpace = true;
        telegraphLine.positionCount = 2;
        telegraphLine.startWidth = 0.08f;
        telegraphLine.endWidth = 0.03f;
        telegraphLine.numCapVertices = 4;
        telegraphLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        telegraphLine.receiveShadows = false;

        Shader lineShader = Shader.Find("Sprites/Default");

        if (lineShader != null)
        {
            telegraphMaterial = new Material(lineShader);
            telegraphLine.material = telegraphMaterial;
        }

        SetTelegraphVisible(false);
    }

    private void CreateSlamTelegraphRing()
    {
        GameObject ringObject = new GameObject("BossGroundSlamTelegraph");
        ringObject.transform.SetParent(transform, false);
        slamTelegraphRing = ringObject.AddComponent<LineRenderer>();
        slamTelegraphRing.useWorldSpace = true;
        slamTelegraphRing.loop = true;
        slamTelegraphRing.positionCount = slamCircleSegments;
        slamTelegraphRing.startWidth = 0.12f;
        slamTelegraphRing.endWidth = 0.12f;
        slamTelegraphRing.numCapVertices = 4;
        slamTelegraphRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        slamTelegraphRing.receiveShadows = false;

        if (telegraphMaterial != null)
        {
            slamTelegraphRing.sharedMaterial = telegraphMaterial;
        }

        SetSlamTelegraphVisible(false);
    }

    private void UpdateSlamTelegraph(float progress, float radius)
    {
        if (slamTelegraphRing == null)
        {
            return;
        }

        Vector3 center = GetSlamCenter() + Vector3.up * 0.06f;
        Color ringColor = Color.Lerp(new Color(1f, 0.78f, 0.08f, 0.75f), new Color(1f, 0.06f, 0.02f, 1f), progress);
        slamTelegraphRing.startColor = ringColor;
        slamTelegraphRing.endColor = ringColor;

        for (int pointIndex = 0; pointIndex < slamCircleSegments; pointIndex++)
        {
            float angle = pointIndex * Mathf.PI * 2f / slamCircleSegments;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            slamTelegraphRing.SetPosition(pointIndex, center + offset);
        }
    }

    private Vector3 GetSlamCenter()
    {
        return groundCenter != null ? groundCenter.position : transform.position;
    }

    private void UpdateTelegraphLine(float progress)
    {
        Transform muzzle = GetProjectileOrigin(0);

        if (telegraphLine == null || target == null || muzzle == null)
        {
            return;
        }

        Color telegraphColor = Color.Lerp(new Color(1f, 0.85f, 0.1f, 0.55f), new Color(1f, 0.1f, 0.05f, 1f), progress);
        telegraphLine.startColor = telegraphColor;
        telegraphLine.endColor = telegraphColor;
        telegraphLine.SetPosition(0, GetMuzzlePosition(muzzle, 0));
        telegraphLine.SetPosition(1, target.position + Vector3.up * targetHeightOffset);
    }

    private void ResolveProjectileOrigins()
    {
        string[] muzzleBoneNames = { "ShotgunTop_R", "ShotgunTop_L", "ShotgunBot_R", "ShotgunBot_L" };
        Transform[] hierarchy = GetComponentsInChildren<Transform>(true);
        Transform[] resolvedOrigins = new Transform[muzzleBoneNames.Length];
        int resolvedCount = 0;

        foreach (string boneName in muzzleBoneNames)
        {
            foreach (Transform candidate in hierarchy)
            {
                if (candidate.name != boneName)
                {
                    continue;
                }

                resolvedOrigins[resolvedCount] = candidate;
                resolvedCount++;
                break;
            }
        }

        if (resolvedCount > 0)
        {
            projectileOrigins = new Transform[resolvedCount];
            Array.Copy(resolvedOrigins, projectileOrigins, resolvedCount);
            Debug.Log($"{name}: Resolved {resolvedCount} animated cannon muzzle bones.", this);
        }
        else
        {
            Debug.LogWarning($"{name}: Cannon bones were not found. Using the fallback ProjectileOrigin anchor.", this);
        }
    }

    private Transform GetProjectileOrigin(int shotIndex)
    {
        if (projectileOrigins != null && projectileOrigins.Length > 0)
        {
            return projectileOrigins[shotIndex % projectileOrigins.Length];
        }

        return projectileOrigin;
    }

    private Vector3 GetMuzzlePosition(Transform muzzle, int shotIndex)
    {
        if (muzzle == null)
        {
            return transform.position;
        }

        if (projectileOrigins != null && projectileOrigins.Length > 0)
        {
            return muzzle.TransformPoint(muzzleLocalOffset);
        }

        float side = shotIndex % 2 == 0 ? -0.5f : 0.5f;
        return muzzle.position + transform.right * side * muzzleSeparation;
    }

    private void SetTelegraphVisible(bool shouldShow)
    {
        if (telegraphLine != null)
        {
            telegraphLine.enabled = shouldShow;
        }
    }

    private void SetSlamTelegraphVisible(bool shouldShow)
    {
        if (slamTelegraphRing != null)
        {
            slamTelegraphRing.enabled = shouldShow;
        }
    }
}
