using UnityEngine;
using System.Collections;

public class EnemyShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform detectionOrigin;
    [SerializeField] private Transform muzzle;
    [SerializeField] private ParticleSystem muzzleFlash;

    [SerializeField] private EnemyProjectilePool projectilePool;

    [Header("Shooting")]
    [SerializeField, Min(0.1f)] private float fireInterval = 1.2f;
    [SerializeField, Min(0f)] private float projectileDelay = 0.15f;
    [SerializeField, Min(0f)] private float maximumAimAngle = 10f;
    [SerializeField] private LayerMask sightLayers = ~0;

    private static readonly int ShootHash = Animator.StringToHash("Shoot");

    private EnemyBrain brain;
    private Animator animator;
    private float nextFireTime;
    private Coroutine fireRoutine;

    private void Awake(){
        brain = GetComponent<EnemyBrain>();
        animator = GetComponentInChildren<Animator>();

        if (detectionOrigin == null)
        {
            Debug.LogError($"{name}: EnemyShooter chưa được gắn DetectionOrigin.");
        }

        if (animator == null)
        {
            Debug.LogError($"{name}: Không tìm thấy Animator.");
        }

        if (projectilePool == null)
        {
            projectilePool = FindFirstObjectByType<EnemyProjectilePool>();
        }

        if (projectilePool == null)
        {
            Debug.LogError($"{name}: Không tìm thấy EnemyProjectilePool trong Scene");
        }

    }   

    private void Update(){

        if (brain == null || animator == null || detectionOrigin == null || muzzle == null || projectilePool == null)
        {
            return;
        }

        if (!brain.CanAttack || Time.time < nextFireTime || !HasLineOfSight() || !IsFacingTarget())
        {
            return;
        }

        nextFireTime = Time.time + fireInterval;
        animator.SetTrigger(ShootHash);

        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
        }

        fireRoutine = StartCoroutine(FireAfterDelay());
    }

    private void OnDisable()
    {
        StopFiring();
    }

    private IEnumerator FireAfterDelay()
    {
        yield return new WaitForSeconds(projectileDelay);

        fireRoutine = null;

        if (brain.Target == null)
        {
            yield break;
        }

        Vector3 targetPoint = brain.Target.position + Vector3.up;

        Vector3 direction = (targetPoint - muzzle.position);

        if (direction.sqrMagnitude < 0.001f)
        {
            yield break;
        }

        direction.Normalize();

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play(true);
        }

        projectilePool.Spawn(transform, muzzle.position, direction);
    }

    private bool HasLineOfSight()
    {
        if (detectionOrigin == null || brain.Target == null)
        {
            return false;
        }

        Vector3 targetPoint = brain.Target.position + Vector3.up;

        Vector3 direction = targetPoint - detectionOrigin.position;

        if (!Physics.Raycast(detectionOrigin.position,direction.normalized,out RaycastHit hit,direction.magnitude,sightLayers,QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return hit.transform == brain.Target || hit.transform.IsChildOf(brain.Target);
    }

    private bool IsFacingTarget()
    {
        Vector3 direction = brain.Target.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return true;
        }

        return Vector3.Angle(transform.forward, direction.normalized) <= maximumAimAngle;
    }

    public void ResetForSpawn()
    {
        StopFiring();

        nextFireTime = Time.time + fireInterval;

        if (animator != null)
        {
            animator.ResetTrigger(ShootHash);
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    public void StopForPool()
    {
        StopFiring();

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void StopFiring()
    {
        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
            fireRoutine = null;
        }
    }
}