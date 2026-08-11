using UnityEngine;
public class Bullet : MonoBehaviour
{
    public float speed = 60f;
    public float damage = 10f;
    public float lifeTime = 3f;

    [Header("Impact Feedback")]
    [SerializeField] private GameObject damageImpactPrefab;
    [SerializeField] private GameObject worldImpactPrefab;
    [SerializeField, Min(0.1f)] private float impactLifetime = 2f;

    void Start() => Destroy(gameObject, lifeTime);

    public void Initialize(float newDamage, float newSpeed)
    {
        damage = Mathf.Max(0f, newDamage);
        speed = Mathf.Max(0f, newSpeed);
    }

    void Update()
    {
        float step = speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, step, ~0, QueryTriggerInteraction.Ignore))
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                HitImpactUtility.Spawn(damageImpactPrefab, hit, impactLifetime);
            }
            else
            {
                HitImpactUtility.Spawn(worldImpactPrefab, hit, impactLifetime);
            }

            Destroy(gameObject);
            return;
        }

        transform.position += transform.forward * step;
    }
}
