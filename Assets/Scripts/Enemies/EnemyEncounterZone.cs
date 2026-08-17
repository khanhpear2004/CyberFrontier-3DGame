using UnityEngine;
using System.Collections.Generic;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class EnemyEncounterZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawnManager spawnManager;
    [SerializeField] private Transform spawnPointsRoot;
    [SerializeField] private Transform guidanceTarget;

    [Header("Activation")]
    [SerializeField] private bool activateOnlyOnce = true;

    [Header("Testing")]
    [SerializeField] private bool suppressEnemySpawning;

    private EnemySpawnPoint[] spawnPoints;
    private bool hasActivated;
    private bool playerInside;
    private bool isCompleted;
    private readonly HashSet<EnemyHealth> aliveEnemies = new();
    public int RemainingEnemyCount => aliveEnemies.Count;
    public bool HasActivated => hasActivated;
    public bool IsCompleted => isCompleted;
    public Transform GuidanceTarget => guidanceTarget != null ? guidanceTarget : transform;

    public event Action<EnemyEncounterZone> EncounterActivated;
    public event Action<EnemyEncounterZone> EncounterCompleted;

    public void SetEnemySpawningSuppressed(bool shouldSuppress)
    {
        if (!hasActivated)
        {
            suppressEnemySpawning = shouldSuppress;
        }
    }

    [ContextMenu("Debug/Complete Encounter")]
    public void DebugCompleteEncounter()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (isCompleted)
        {
            return;
        }

        hasActivated = true;

        if (aliveEnemies.Count == 0)
        {
            CompleteEncounter();
            return;
        }

        List<EnemyHealth> enemies = new(aliveEnemies);

        foreach (EnemyHealth enemyHealth in enemies)
        {
            if (enemyHealth != null && !enemyHealth.IsDead)
            {
                enemyHealth.TakeDamage(float.MaxValue);
            }
        }

        if (!isCompleted)
        {
            StopTrackingAllEnemies();
            CompleteEncounter();
        }
#endif
    }

    private void Awake()
    {
        if (spawnManager == null)
        {
            spawnManager = FindAnyObjectByType<EnemySpawnManager>();
        }

        if (spawnPointsRoot != null)
        {
            spawnPoints = spawnPointsRoot.GetComponentsInChildren<EnemySpawnPoint>(true);
        }

        BoxCollider trigger = GetComponent<BoxCollider>();

        if (!trigger.isTrigger)
        {
            Debug.LogError($"{name}: Box Collider phải bật Is Trigger.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerInside || !IsPlayer(other))
        {
            return;
        }

        playerInside = true;
        ActivateEncounter();
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInside = false;
        }
    }

    private void OnDestroy()
    {
        StopTrackingAllEnemies();
    }

    private void ActivateEncounter()
    {
        if (suppressEnemySpawning)
        {
            hasActivated = true;
            isCompleted = false;
            Debug.Log($"{name}: Boss-only test mode. Enemy spawning skipped.", this);
            EncounterActivated?.Invoke(this);
            CompleteEncounter();
            return;
        }

        if (spawnManager == null)
        {
            Debug.LogError($"{name}: Không tìm thấy EnemySpawnManager.", this);
            return;
        }

        if (activateOnlyOnce && hasActivated)
        {
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: Không có EnemySpawnPoint.", this);
            return;
        }

        StopTrackingAllEnemies();

        hasActivated = true;
        isCompleted = false;

        foreach (EnemySpawnPoint spawnPoint in spawnPoints)
        {
            EnemyPoolMember enemySpawn = spawnManager.Spawn(spawnPoint);

            if (enemySpawn == null)
            {
                continue;
            }

            EnemyHealth enemyHealth = enemySpawn.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                TrackEnemy(enemyHealth);
            }
        }

        if (aliveEnemies.Count == 0)
        {
            hasActivated = false;
            return;
        }

        Debug.Log($"{name}: Encounter bắt đầu với {aliveEnemies.Count} enemy.", this);
        EncounterActivated?.Invoke(this);
    }

    private void HandleEnemyDied(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Died -= HandleEnemyDied;

        bool wasRemoved = aliveEnemies.Remove(enemyHealth);

        if (!wasRemoved)
        {
            return;
        }

        Debug.Log($"{name}: Một enemy đã bị tiêu diệt. Còn lại {aliveEnemies.Count}.", this);

        if (aliveEnemies.Count == 0)
        {
            CompleteEncounter();
        }
        
    }

    private void CompleteEncounter()
    {
        if (isCompleted)
        {
            return;
        }

        isCompleted = true;

        Debug.Log($"{name}: Encounter đã hoàn thành.", this);
        EncounterCompleted?.Invoke(this);
    }

    private void TrackEnemy(EnemyHealth enemyHealth)
    {
        if (enemyHealth == null)
        {
            return;
        }

        bool wasAdded = aliveEnemies.Add(enemyHealth);

        if (!wasAdded)
        {
            return;
        }
        enemyHealth.Died += HandleEnemyDied;
    }

    private void StopTrackingAllEnemies()
    {
        foreach (EnemyHealth enemyHealth in aliveEnemies)
        {
            if (enemyHealth != null)
            {
                enemyHealth.Died -= HandleEnemyDied;
            }
        }

        aliveEnemies.Clear();
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }
}
