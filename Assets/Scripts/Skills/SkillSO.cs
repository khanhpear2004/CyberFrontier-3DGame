using System.Collections;
using UnityEngine;

// Lớp NỀN cho mọi chiêu. Mỗi chiêu là một asset ScriptableObject riêng.
// Muốn đổi chiêu (nhặt lõi xịn hơn) = gán một SkillSO khác vào nút -> xem SkillCooldownButton.SetSkill().
public abstract class SkillSO : ScriptableObject
{
    [Header("Thông tin chung")]
    public string skillName = "Skill";
    public Sprite icon;                 // Icon hiển thị trên nút bấm

    [Header("Chi phí dùng chiêu")]
    public float cooldownDuration = 5f; // Hồi chiêu (giây). Để 0 nếu chiêu dùng năng lượng.
    public float energyCost = 0f;       // Tốn năng lượng mỗi lần dùng. >0 = gate bằng năng lượng thay vì hồi chiêu.

    // Mỗi loại chiêu tự định nghĩa cách thực thi.
    // Dùng IEnumerator để hỗ trợ chiêu KÉO DÀI (bật pose -> chờ -> tắt).
    // Coroutine sẽ được nút skill chạy giúp (xem SkillCooldownButton).
    public abstract IEnumerator Execute(SkillContext ctx);
}
