using System;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyPoolMember : MonoBehaviour
{
    private EnemyBrain brain;
    private EnemyShooter shooter;

    private EnemyHealth health;
    private Action<EnemyPoolMember> releaseCallback;
    private bool isReleased;

    private NavMeshAgent agent;
    private EnemySpawnTransition spawnTransition;

    private void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        shooter = GetComponent<EnemyShooter>();
        health = GetComponent<EnemyHealth>();

        agent = GetComponent<NavMeshAgent>();
        spawnTransition = GetComponent<EnemySpawnTransition>();
    }

    public void Activate(Action<EnemyPoolMember> onRelease)
    {
        releaseCallback = onRelease;
        isReleased = false;

        gameObject.SetActive(true);

        health?.ResetForSpawn();
        brain?.ResetForSpawn();
        shooter?.ResetForSpawn();

        SetGameplayEnabled(false);

        if (spawnTransition != null)
        {
            spawnTransition.Play(FinishSpawn);
        }
        else
        {
            FinishSpawn();
        }
    }

    public void Despawn()
    {
        if (isReleased)
        {
            return;
        }

        isReleased = true;
        shooter?.StopForPool();

        spawnTransition?.ResetForPool();
        SetGameplayEnabled(false);

        Action<EnemyPoolMember> callback = releaseCallback;
        releaseCallback = null;

        if (callback != null)
        {
            callback(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void FinishSpawn()
    {
        if (isReleased || !gameObject.activeInHierarchy)
        {
            return;
        }

        SetGameplayEnabled(true);
    }

    private void SetGameplayEnabled(bool value)
    {
        if (brain != null)
        {
            brain.enabled = value;
        }

        if (shooter != null)
        {
            shooter.enabled = value;
        }

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = !value;

            if (!value)
            {
                agent.ResetPath();
            }
        }
    }
}