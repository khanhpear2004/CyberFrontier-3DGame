using UnityEngine;
using Unity.Cinemachine;

public class PlayerAimShooter : MonoBehaviour
{
    [Header("Phím")]
    public KeyCode aimToggleKey = KeyCode.Q;

    [Header("Camera & Tâm ngắm")]
    public CinemachineCamera aimCamera;   // AimCamera (Priority nâng lên khi ngắm)
    public int aimPriority = 20;          // Priority lúc đang ngắm
    public int normalPriority = 0;        // Priority lúc thường
    public GameObject crosshair;          // UI tâm ngắm (bật khi ngắm)
    public Camera viewCamera;             // Camera thật để raycast (để trống = Camera.main)

    [Header("Animator")]
    public Animator animator;
    public string aimingBool = "isBeaming"; // dùng lại pose beam làm pose ngắm

    [Header("Bắn đạn")]
    public Bullet bulletPrefab;           // Prefab viên đạn (có script Bullet)
    public Transform muzzle;              // Điểm bắn ra (đầu nòng / tay robot)
    public float fireRate = 10f;          // Số viên / giây
    public float energyPerShot = 5f;      // Năng lượng mỗi viên
    public float aimRange = 200f;         // Tầm raycast tìm điểm ngắm

    private PlayerEnergy energy;
    private CinemachineBrain brain;
    private PlayerController playerController;

    [SerializeField] private RuntimeStats runtimeStats;
    private bool isAiming = false;
    private float fireTimer = 0f;

    void Start()
    {
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera != null) viewCamera.TryGetComponent(out brain);
        if (animator == null) animator = GetComponentInChildren<Animator>();

        playerController = GetComponent<PlayerController>();
        energy = GetComponent<PlayerEnergy>();
        if (energy == null) energy = FindFirstObjectByType<PlayerEnergy>();
        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<RuntimeStats>();
        }

        SetAim(false);
    }

    void Update()
    {
        if (RewardPopupUI.IsGameplayInputBlocked || InventoryMenuUI.IsOpen)
        {
            return;
        }

        if (playerController != null && playerController.isFlying)
        {
            if (isAiming) SetAim(false); 
            return; 
        }
        
        if (Input.GetKeyDown(aimToggleKey)) SetAim(!isAiming);

        bool aimLive = isAiming && IsAimCameraLive();

        if (crosshair != null && crosshair.activeSelf != aimLive)
            crosshair.SetActive(aimLive);

        if (aimLive)
        {
            if (fireTimer > 0f) fireTimer -= Time.deltaTime;

            if (Input.GetMouseButton(0) && fireTimer <= 0f)
            {
                 if (TryFire())
                {
                    float currentFireRate = GetCurrentStat(StatType.FireRate, fireRate);
                    fireTimer = 1f / Mathf.Max(0.01f, currentFireRate);
                }
            }
        }
    }

    void SetAim(bool on)
    {
        isAiming = on;

        if (aimCamera != null) aimCamera.Priority = on ? aimPriority : normalPriority;
        if (animator != null) animator.SetBool(aimingBool, on);
        if (playerController != null) playerController.SetAiming(on); 
    }

    bool IsAimCameraLive()
    {
        if (brain == null || aimCamera == null) return true;
        return !brain.IsBlending && ReferenceEquals(brain.ActiveVirtualCamera, aimCamera);
    }

    bool TryFire()
    {
        if (bulletPrefab == null || muzzle == null || viewCamera == null) return false;

        if (energy != null && !energy.TrySpend(energyPerShot)) return false;

        Vector3 aimPoint = GetAimPoint();
        Vector3 dir = (aimPoint - muzzle.position).normalized;

        float finalDamage = GetCurrentStat(StatType.Damage, bulletPrefab.damage);
        float finalProjectileSpeed = GetCurrentStat(StatType.ProjectileSpeed, bulletPrefab.speed);

        Bullet bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(dir));
        bullet.Initialize(finalDamage, finalProjectileSpeed);

        return true;
    }

    Vector3 GetAimPoint()
    {
        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        var hits = Physics.RaycastAll(ray, aimRange, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue; // bỏ qua chính người chơi
            return h.point;
        }
        return ray.origin + ray.direction * aimRange; // không trúng gì -> bắn ra xa
    }

    private float GetCurrentStat(StatType statType, float fallbackValue)
    {
        if (runtimeStats == null)
        {
            return fallbackValue;
        }

        return runtimeStats.GetValue(statType);
    }

    public void CancelAim()
    {
        if (isAiming)
        {
            SetAim(false);
        }
    }
}
