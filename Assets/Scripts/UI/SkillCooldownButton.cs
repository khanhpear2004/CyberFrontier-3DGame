using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // cho IPointerClickHandler (nhận click chuột)
using TMPro;

// Một nút chiêu thức kiểu ZZZ: dùng HỒI CHIÊU (cooldown) thay vì giới hạn đạn.
// LIÊN KẾT phím tắt + click chuột: cả hai đều gọi chung TryUse().
public class SkillCooldownButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Thông tin chiêu")]
    public string skillName = "Skill";
    public KeyCode activationKey = KeyCode.Q;   // Phím tắt kích hoạt
    public float cooldownDuration = 5f;          // Thời gian hồi (giây)

    [Header("Tham chiếu UI")]
    public Image cooldownOverlay;   // Lớp tối phủ icon (Image Type = Filled, Radial 360)
    public TMP_Text cooldownText;   // Số giây còn lại (tùy chọn)

    private float cooldownRemaining = 0f;

    public bool IsReady => cooldownRemaining <= 0f;

    void Update()
    {
        // (1) Bàn phím: ấn phím tắt
        if (Input.GetKeyDown(activationKey))
        {
            TryUse();
        }

        // Đếm ngược thời gian hồi
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            if (cooldownRemaining < 0f) cooldownRemaining = 0f;
        }

        UpdateVisual();
    }

    // (2) Click chuột vào nút trên UI -> cũng gọi chung TryUse()
    public void OnPointerClick(PointerEventData eventData)
    {
        TryUse();
    }

    // Dùng chung cho cả PHÍM lẫn CLICK: chỉ kích hoạt nếu đã hồi xong
    public void TryUse()
    {
        if (!IsReady) return;
        Activate();
    }

    // Kích hoạt chiêu + bắt đầu hồi
    public void Activate()
    {
        cooldownRemaining = cooldownDuration;
        Debug.Log("Dùng chiêu: " + skillName);
        // TODO: gọi hiệu ứng / sát thương / animation của chiêu ở đây
    }

    // Cập nhật hiển thị hồi chiêu
    void UpdateVisual()
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount =
                (cooldownDuration > 0f) ? cooldownRemaining / cooldownDuration : 0f;
        }

        if (cooldownText != null)
        {
            cooldownText.text = cooldownRemaining > 0f
                ? Mathf.Ceil(cooldownRemaining).ToString()
                : "";
        }
    }
}
