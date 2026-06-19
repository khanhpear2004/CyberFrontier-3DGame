using UnityEngine;

// Viên đạn năng lượng: bay thẳng theo hướng được bắn, quét tia để phát hiện trúng (tránh xuyên khi bay nhanh).
// KHÔNG cần Rigidbody/Collider trên prefab đạn (dùng raycast).
public class Bullet : MonoBehaviour
{
    public float speed = 60f;
    public float damage = 10f;
    public float lifeTime = 3f;

    void Start() => Destroy(gameObject, lifeTime); // tự hủy nếu bay mãi không trúng

    void Update()
    {
        float step = speed * Time.deltaTime;

        // Quét tia từ vị trí hiện tại về phía trước một đoạn = quãng đường frame này
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, step,
                            ~0, QueryTriggerInteraction.Ignore))
        {
            var hp = hit.collider.GetComponentInParent<Health>();
            if (hp != null) hp.TakeDamage(damage);

            // TODO: spawn hiệu ứng nổ tại hit.point
            Destroy(gameObject);
            return;
        }

        transform.position += transform.forward * step;
    }
}
