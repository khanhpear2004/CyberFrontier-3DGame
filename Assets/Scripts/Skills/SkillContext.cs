using UnityEngine;

// Gói các tham chiếu mà MỘT chiêu cần khi thực thi.
// Tách riêng ra để sau này thêm gì (điểm bắn, hệ thống sát thương, mục tiêu...)
// thì chỉ cần bổ sung vào đây, không phải sửa từng chiêu.
public class SkillContext
{
    public Animator animator;     // Animator của nhân vật (để bật/tắt tư thế chiêu)
    public Transform player;      // Transform người chơi (spawn VFX, xác định hướng...)
    public PlayerEnergy energy;   // Năng lượng người chơi (chiêu tốn năng lượng dùng tới)
    public MonoBehaviour runner;  // Đối tượng chạy Coroutine (chính là nút skill)
}
