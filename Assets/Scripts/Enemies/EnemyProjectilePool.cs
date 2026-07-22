using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public class EnemyProjectilePool : MonoBehaviour
{
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField, Min(1)] private int initialSize = 20;
    [SerializeField, Min(1)] private int maximumSize = 50;

    private IObjectPool<EnemyProjectile> pool;

    private void Awake()
    {
        maximumSize = Mathf.Max(maximumSize, initialSize);

        pool = new ObjectPool<EnemyProjectile>(
            CreateProjectile,
            null,
            OnReleaseProjectile,
            OnDestroyProjectile,
            true,
            initialSize,
            maximumSize
        );

        Prewarm();
    }

    public void Spawn(Transform owner, Vector3 position, Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        direction.Normalize();

        EnemyProjectile projectile = pool.Get();

        projectile.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));

        projectile.Initialize(owner, direction, Release);
        
        projectile.gameObject.SetActive(true);
    }

    private EnemyProjectile CreateProjectile()
    {
        EnemyProjectile projectile = Instantiate(projectilePrefab, transform);

        projectile.gameObject.SetActive(false);
        return projectile;
    }

    private void Release(EnemyProjectile projectile)
    {
        pool.Release(projectile);
    }

    private void OnReleaseProjectile(EnemyProjectile projectile)
    {
        projectile.gameObject.SetActive(false);
        projectile.transform.SetParent(transform);
    }

    private void OnDestroyProjectile(EnemyProjectile projectile)
    {
        Destroy(projectile.gameObject);
    }

    private void Prewarm()
    {
        List<EnemyProjectile> projectiles = new List<EnemyProjectile>(initialSize);

        for (int i = 0; i < initialSize; i++) projectiles.Add(pool.Get());

        foreach (EnemyProjectile projectile in projectiles) pool.Release(projectile);
    }
}