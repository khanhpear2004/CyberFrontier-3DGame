using UnityEngine;

// Máu cho MỤC TIÊU (kẻ địch, vật phá được...). Đạn trúng vật có component này thì trừ máu.
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    public System.Action OnDeath;

    void Start() => currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDeath?.Invoke();
            Destroy(gameObject); // tạm: chết là biến mất (sau này thay bằng hiệu ứng nổ...)
        }
    }
}
