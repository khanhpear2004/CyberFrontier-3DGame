using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBrain : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Aim
    }
    [Header("Group Avoidance")]
    [SerializeField] private Vector2Int avoidancePriorityRange = new Vector2Int(30, 70);

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float detectionRange = 15f;
    [SerializeField, Min(0.1f)] private float loseRange = 20f;
    [SerializeField, Min(0.1f)] private float attackRange = 6f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float rotationSpeed = 240f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    public Transform Target => target;
    public bool CanAttack => currentState == EnemyState.Aim && target != null;
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyState currentState = (EnemyState)(-1);
    
    private float nextRepathTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    private void Start()
    {
        if(currentState == (EnemyState)(-1))
        {
            ResetForSpawn();
        }
    }

    private void Update()
    {
        if (target == null || animator == null)
        {
            return;
        }

        UpdateState();
        UpdateAnimation();
    }

    private void UpdateState()
    {
        float distance = Vector3.Distance(transform.position, target.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                if (distance <= detectionRange)
                {
                    ChangeState(EnemyState.Chase);
                }
                break;

            case EnemyState.Chase:
                if (distance > loseRange)
                {
                    ChangeState(EnemyState.Idle);
                }
                else if (distance <= attackRange)
                {
                    ChangeState(EnemyState.Aim);
                }
                else
                {
                    FollowTarget();
                }
                break;

            case EnemyState.Aim:
                if (distance > loseRange)
                {
                    ChangeState(EnemyState.Idle);
                }
                else if (distance > attackRange + 1f)
                {
                    ChangeState(EnemyState.Chase);
                }
                else
                {
                    RotateTowardsTarget();
                }
                break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        switch (currentState)
        {
            case EnemyState.Idle:
                StopAgent();
                animator.SetBool(IsAimingHash, false);
                break;

            case EnemyState.Chase:
                ResumeAgent();
                animator.SetBool(IsAimingHash, false);
                break;

            case EnemyState.Aim:
                StopAgent();
                animator.SetBool(IsAimingHash, true);
                break;
        }
    }

    private void FollowTarget()
    {
        if (!agent.isOnNavMesh || Time.time < nextRepathTime)
        {
            return;
        }

        nextRepathTime = Time.time + repathInterval;
        agent.SetDestination(target.position);
    }

    private void RotateTowardsTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void StopAgent()
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void ResumeAgent()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    private void UpdateAnimation()
    {
        float normalizedSpeed = 0f;

        if (agent.speed > 0f)
        {
            normalizedSpeed = agent.velocity.magnitude / agent.speed;
        }

        animator.SetFloat(
            SpeedHash,
            normalizedSpeed,
            0.1f,
            Time.deltaTime
        );
    }

    public void ResetForSpawn()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
        }

        nextRepathTime = 0f;
        currentState = (EnemyState)(-1);

        if (agent != null)
        {
            int minimumPriority = Mathf.Clamp(avoidancePriorityRange.x, 0, 99);

            int maximumPriority = Mathf.Clamp(avoidancePriorityRange.y, minimumPriority, 99);

            agent.avoidancePriority = Random.Range(minimumPriority, maximumPriority + 1);
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsAimingHash, false);
        }

        ChangeState(EnemyState.Idle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}