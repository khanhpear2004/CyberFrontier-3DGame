using System.Collections;
using UnityEngine;

// CHIÊU MẶC ĐỊNH: bắn Beam.
// Bật bool "isBeaming" trên Animator -> giữ tư thế bắn trong beamDuration giây -> tắt.
// Tạo asset: chuột phải trong Project -> Create -> Game3D/Skills/Beam Skill.
[CreateAssetMenu(fileName = "BeamSkill", menuName = "Game3D/Skills/Beam Skill")]
public class BeamSkillSO : SkillSO
{
    [Header("Thiết lập Beam")]
    public string animatorBool = "isBeaming"; // Tên Bool trong Animator (phải khớp y hệt)
    public float beamDuration = 3f;            // Bắn beam mấy giây rồi tự tắt

    public override IEnumerator Execute(SkillContext ctx)
    {
        if (ctx.animator == null) yield break;

        ctx.animator.SetBool(animatorBool, true);   // Dựng + giữ tư thế beam
        // TODO: bật VFX beam / gây sát thương ở đây

        yield return new WaitForSeconds(beamDuration);

        ctx.animator.SetBool(animatorBool, false);  // Hạ tay, kết thúc chiêu
        // TODO: tắt VFX beam ở đây
    }
}
