using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public class EnemySpawnManager : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private EnemyPoolMember enemyPrefab;
    [SerializeField] private Transform runtimeParent;
    [SerializeField, Min(1)] private int initialSize = 3;
    [SerializeField, Min(1)] private int maximumSize = 20;

    [Header("Spawn")]
    [SerializeField, Min(0.1f)]
    private float navMeshSampleDistance = 2f;

    private IObjectPool<EnemyPoolMember> pool;

    private void Awake()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError($"{name}: Chưa gắn Enemy Prefab.", this);
            enabled = false;
            return;
        }

        maximumSize = Mathf.Max(maximumSize, initialSize);

        pool = new ObjectPool<EnemyPoolMember>(
            CreateEnemy,
            null,
            OnReleaseEnemy,
            OnDestroyEnemy,
            true,
            initialSize,
            maximumSize
        );

        Prewarm();
    }

    public EnemyPoolMember Spawn(EnemySpawnPoint spawnPoint)
    {
        if (pool == null || spawnPoint == null)
        {
            return null;
        }

        if (!NavMesh.SamplePosition(spawnPoint.Position,out NavMeshHit hit, navMeshSampleDistance,NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: Spawn point {spawnPoint.name} không gần NavMesh.", spawnPoint);
            return null;
        }

        EnemyPoolMember enemy = pool.Get();

        enemy.transform.SetPositionAndRotation(hit.position, spawnPoint.Rotation);

        enemy.transform.SetParent(runtimeParent != null ? runtimeParent : transform);

        enemy.Activate(Release);
        return enemy;
    }

    private EnemyPoolMember CreateEnemy()
    {
        Transform parent = runtimeParent != null ? runtimeParent : transform;

        EnemyPoolMember enemy = Instantiate(enemyPrefab, parent);

        enemy.gameObject.SetActive(false);
        return enemy;
    }

    private void Release(EnemyPoolMember enemy)
    {
        pool.Release(enemy);
    }

    private void OnReleaseEnemy(EnemyPoolMember enemy)
    {
        enemy.gameObject.SetActive(false);

        enemy.transform.SetParent(runtimeParent != null ? runtimeParent : transform);
    }

    private void OnDestroyEnemy(EnemyPoolMember enemy)
    {
        Destroy(enemy.gameObject);
    }

    private void Prewarm()
    {
        List<EnemyPoolMember> enemies = new List<EnemyPoolMember>(initialSize);

        for (int i = 0; i < initialSize; i++)
        {
            enemies.Add(pool.Get());
        }

        foreach (EnemyPoolMember enemy in enemies)
        {
            pool.Release(enemy);
        }
    }
}