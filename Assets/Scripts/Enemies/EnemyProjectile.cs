using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float speed = 25f;
    [SerializeField, Min(0.1f)] private float lifeTime = 4f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers = ~0;

    [Header("Damage")]
    [SerializeField, Min(0f)] private float damage = 10f;

    [Header("Impact Feedback")]
    [SerializeField] private GameObject playerImpactPrefab;
    [SerializeField] private GameObject dashAvoidedImpactPrefab;
    [SerializeField] private GameObject worldImpactPrefab;
    [SerializeField, Min(0.1f)] private float impactLifetime = 2f;

    [Header("Impact Audio")]
    [SerializeField] private AudioCue playerImpactCue;
    [SerializeField] private AudioCue dashAvoidedImpactCue;
    [SerializeField] private AudioCue worldImpactCue;

    private Transform owner;
    private float remainingLifeTime;
    private Action <EnemyProjectile> releaseCallback;

    public void Initialize(Transform projectileOwner, Vector3 direction, Action<EnemyProjectile> releaseCallback)
    {
        owner = projectileOwner;
        remainingLifeTime = lifeTime;
        this.releaseCallback = releaseCallback;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }

    private void OnEnable()
    {
        remainingLifeTime = lifeTime;
    }

    private void Update()
    {
        float movementDistance = speed * Time.deltaTime;

        if (CheckCollision(movementDistance))
        {
            return;
        }

        if (!gameObject.activeSelf)
        {
            return;
        }

        transform.position += transform.forward * movementDistance;

        remainingLifeTime -= Time.deltaTime;

        if (remainingLifeTime <= 0f)
        {
            //Destroy(gameObject);
            ReturnToPool();
        }
    }

    private bool CheckCollision(float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position,transform.forward,distance,collisionLayers,QueryTriggerInteraction.Ignore);

        Array.Sort(
            hits,
            (first, second) => first.distance.CompareTo(second.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (BelongsToOwner(hit.transform))
            {
                continue;
            }

            Debug.Log($"{name} trúng {hit.collider.name}", hit.collider);

            transform.position = hit.point;

            PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                bool wasDashAvoided = playerHealth.IsDashInvulnerable;

                playerHealth.TakeDamage(damage);

                GameObject impactPrefab = wasDashAvoided && dashAvoidedImpactPrefab != null
                    ? dashAvoidedImpactPrefab
                    : playerImpactPrefab;

                HitImpactUtility.Spawn(impactPrefab, hit, impactLifetime);
                AudioCue impactCue = wasDashAvoided && dashAvoidedImpactCue != null
                    ? dashAvoidedImpactCue
                    : playerImpactCue;
                PlayImpactAudio(impactCue, hit.point);
            }
            else
            {
                HitImpactUtility.Spawn(worldImpactPrefab, hit, impactLifetime);
                PlayImpactAudio(worldImpactCue, hit.point);
            }

            // Destroy(gameObject); 
            ReturnToPool();

            return true;
        }

        return false;
    }

    private static void PlayImpactAudio(AudioCue cue, Vector3 position)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAt(cue, position);
        }
    }

    private bool BelongsToOwner(Transform hitTransform)
    {
        if (owner == null)
        {
            return false;
        }

        return hitTransform == owner || hitTransform.IsChildOf(owner);
    }

    private void ReturnToPool()
    {
        Action<EnemyProjectile> callback = releaseCallback;

        releaseCallback = null;
        owner = null;

        if (callback != null) callback(this);
        else Destroy(gameObject);
    }
}
