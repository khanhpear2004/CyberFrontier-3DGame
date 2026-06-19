using System.Collections;
using UnityEngine;

// Lớp NỀN cho mọi chiêu. Mỗi chiêu là một asset ScriptableObject riêng.
// Muốn đổi chiêu (nhặt lõi xịn hơn) = gán một SkillSO khác vào nút -> xem SkillCooldownButton.SetSkill().
public abstract class SkillSO : ScriptableObject
{
    [Header("Thông tin chung")]
    public string skillName = "Skill";
    public Sprite icon;                 // Icon hiển thị trên nút bấm
    public float cooldownDuration = 5f; // Thời gian hồi chiêu (giây)

    // Mỗi loại chiêu tự định nghĩa cách thực thi.
    // Dùng IEnumerator để hỗ trợ chiêu KÉO DÀI (bật pose -> chờ -> tắt).
    // Coroutine sẽ được nút skill chạy giúp (xem SkillCooldownButton).
    public abstract IEnumerator Execute(SkillContext ctx);
}
