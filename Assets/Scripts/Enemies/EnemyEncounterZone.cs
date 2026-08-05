using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class EnemyEncounterZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawnManager spawnManager;
    [SerializeField] private Transform spawnPointsRoot;

    [Header("Activation")]
    [SerializeField] private bool activateOnlyOnce = true;

    private EnemySpawnPoint[] spawnPoints;
    private bool hasActivated;
    private bool playerInside;
    private bool isCompleted;
    private readonly HashSet<EnemyHealth> aliveEnemies = new();
    public int RemainingEnemyCount => aliveEnemies.Count;
    public bool HasActivated => hasActivated;
    public bool IsCompleted => isCompleted;

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

            if(enemySpawn == null)
            {
                continue;
            }

            if(enemySpawn != null)
            {
                EnemyHealth enemyHealth = enemySpawn.GetComponent<EnemyHealth>();

                if (enemyHealth != null)
                {
                    TrackEnemy(enemyHealth);
                }
            }
        }

        if(aliveEnemies.Count == 0)
        {
            hasActivated = false;
            return;
        }

        Debug.Log($"{name}: Encounter bắt đầu với {aliveEnemies.Count} enemy.", this);
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