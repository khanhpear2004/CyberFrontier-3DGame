using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // cho IPointerClickHandler (nhận click chuột)
using TMPro;

// Nút chiêu kiểu ZZZ: dùng HỒI CHIÊU (cooldown), không giới hạn đạn.
// Phím tắt + click chuột đều gọi chung TryUse().
// Chiêu được NẠP từ một SkillSO -> đổi chiêu (nhặt lõi mới) chỉ cần gọi SetSkill().
public class SkillCooldownButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Chiêu hiện tại (kéo asset SkillSO vào, vd: BeamSkill)")]
    public SkillSO skill;

    [Header("Tham chiếu nhân vật")]
    public Animator playerAnimator;   // Animator của CyberSoldier (để trống sẽ tự tìm)
    public Transform player;          // Transform người chơi (tùy chọn)

    [Header("Phím tắt")]
    public KeyCode activationKey = KeyCode.Q;

    [Header("Tham chiếu UI")]
    public Image iconImage;           // Icon chiêu (tự cập nhật theo skill, tùy chọn)
    public Image cooldownOverlay;     // Lớp tối phủ icon (Image Type = Filled, Radial 360)
    public TMP_Text cooldownText;     // Số giây còn lại (tùy chọn)

    private float cooldownRemaining = 0f;
    private bool isCasting = false;   // Đang chạy chiêu kéo dài -> chặn bấm chồng

    // Chỉ dùng được khi: đã hồi xong VÀ không đang trong lúc tung chiêu
    public bool IsReady => cooldownRemaining <= 0f && !isCasting;

    void Start()
    {
        // Nếu quên gán Animator, tự tìm trên nhân vật có PlayerController
        if (playerAnimator == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) playerAnimator = pc.GetComponent<Animator>();
        }

        RefreshIcon();
    }

    void Update()
    {
        // (1) Bàn phím: ấn phím tắt
        if (Input.GetKeyDown(activationKey)) TryUse();

        // Đếm ngược thời gian hồi
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            if (cooldownRemaining < 0f) cooldownRemaining = 0f;
        }

        UpdateVisual();
    }

    // (2) Click chuột vào nút trên UI -> cũng gọi chung TryUse()
    public void OnPointerClick(PointerEventData eventData) => TryUse();

    // Dùng chung cho cả PHÍM lẫn CLICK: chỉ kích hoạt nếu có chiêu và đã sẵn sàng
    public void TryUse()
    {
        if (skill == null || !IsReady) return;
        StartCoroutine(RunSkill());
    }

    // Chạy chiêu: bắt đầu hồi ngay khi tung, rồi thực thi nội dung chiêu
    private IEnumerator RunSkill()
    {
        isCasting = true;
        cooldownRemaining = skill.cooldownDuration;

        var ctx = new SkillContext { animator = playerAnimator, player = player, runner = this };
        yield return StartCoroutine(skill.Execute(ctx));

        isCasting = false;
    }

    // === Gọi hàm này khi NHẶT LÕI MỚI để đổi sang chiêu khác ===
    public void SetSkill(SkillSO newSkill)
    {
        skill = newSkill;
        cooldownRemaining = 0f;
        isCasting = false;
        RefreshIcon();
    }

    void RefreshIcon()
    {
        if (iconImage != null && skill != null && skill.icon != null)
            iconImage.sprite = skill.icon;
    }

    // Cập nhật hiển thị hồi chiêu
    void UpdateVisual()
    {
        float cd = (skill != null) ? skill.cooldownDuration : 1f;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = (cd > 0f) ? cooldownRemaining / cd : 0f;

        if (cooldownText != null)
            cooldownText.text = cooldownRemaining > 0f
                ? Mathf.Ceil(cooldownRemaining).ToString()
                : "";
    }

    // An toàn: nếu nút bị tắt lúc đang bắn, tắt luôn pose beam kẻo bị kẹt
    void OnDisable()
    {
        if (isCasting && skill is BeamSkillSO beam && playerAnimator != null)
            playerAnimator.SetBool(beam.animatorBool, false);
        isCasting = false;
    }
}
