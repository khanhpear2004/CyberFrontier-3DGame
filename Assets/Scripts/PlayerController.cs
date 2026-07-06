using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float flySpeed = 5.0f;
    [Tooltip("Thời gian làm mượt khi xoay (giây). Nhỏ = xoay nhanh & gắt, lớn = mượt & chậm. ~0.1 là mượt mà nhạy.")]
    public float turnSmoothTime = 0.1f;
    public float gravity = -9.81f;

    [Header("Aim Mode (chế độ ngắm bắn)")]
    public float aimTurnSpeed = 200f;                // Độ nhạy xoay nhân vật bằng chuột khi ngắm
    [HideInInspector] public bool isAiming = false;  // PlayerAimShooter bật/tắt qua SetAiming()

    [Header("Fly Toggle (bật/tắt bay lơ lửng)")]
    public KeyCode flyToggleKey;
    private bool isFlying = false;

    [Header("Flight Controls (Lớp Con & Lực đẩy)")]
    public Transform modelTransform; // Kéo object CyberSoldierModel vào đây ở Inspector!
    public float ascendSpeed = 4.0f;

    [Header("Flight Angles (Chỉnh góc nghiêng X khi bay)")]
    public float hoverPitch = -90f;        // Bắt đầu bay, lơ lửng (đứng thẳng)
    public float forwardPitch = 90f;     // Di chuyển ngang (Superman)
    public float upPitch = -30f;         // Vừa bay vừa đi lên (Ngửa lên)
    public float downPitch = 120f;       // Vừa bay vừa đi xuống (Chúi xuống)
    public float pitchSmoothSpeed = 4f;  // Độ mượt khi chuyển tư thế

    [Header("Camera Info")]
    public Transform mainCamera;

    [Header("Camera Zoom Settings (Cinemachine 3.x)")]
    public CinemachineCamera cinemachineCamera;
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 12f;

    [Header("Zoom cho FreeLook / Orbital Follow (hệ số nhân khoảng cách)")]
    public float orbitalZoomSpeed = 3f;     // Tốc độ zoom khi lăn chuột
    public float minZoomMultiplier = 0.4f;  // Gần nhất (×0.4 khoảng cách gốc)
    public float maxZoomMultiplier = 1.2f;  // Xa nhất (×1.2 khoảng cách gốc)

    [Header("Thruster VFX (Hiệu ứng hạt phản lực)")]
    public ParticleSystem leftThruster;
    public ParticleSystem rightThruster;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private float turnSmoothVelocity; // dùng cho SmoothDampAngle (đừng chỉnh tay)

    private CinemachineThirdPersonFollow thirdPersonFollow;
    private CinemachinePositionComposer positionComposer;
    private CinemachineOrbitalFollow orbitalFollow;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }

        // Lấy component điều khiển vị trí trong Cinemachine 3.x
        if (cinemachineCamera != null)
        {
            // Cinemachine 3.x sử dụng TryGetComponent để lấy các Behaviour
            cinemachineCamera.TryGetComponent(out thirdPersonFollow);
            cinemachineCamera.TryGetComponent(out positionComposer);
            cinemachineCamera.TryGetComponent(out orbitalFollow);

            // FreeLook/Orbital: mặc định RadialAxis bị khoá ở (1,1) -> mở range cho phép zoom
            if (orbitalFollow != null)
            {
                orbitalFollow.RadialAxis.Range = new Vector2(minZoomMultiplier, maxZoomMultiplier);
                orbitalFollow.RadialAxis.Value = Mathf.Clamp(1f, minZoomMultiplier, maxZoomMultiplier);
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ToggleThrusters(false);
    }

    void Update()
    {
        HandleMovement();
        HandleCameraZoom();
        HandleFlying();
    }

    private void HandleFlying()
    {
        if (modelTransform != null)
        {
            float targetPitch = 0f; // Trạng thái mặc định khi đi bộ

            if (animator != null)
            {
                animator.SetBool("isFlying", isFlying);
            }

            if (isFlying)
            {
                float moveMagnitude = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
                float flyVertical = 0f;
                if (Input.GetKey(KeyCode.Space)) flyVertical = 1f;       // Bay lên
                if (Input.GetKey(KeyCode.LeftControl)) flyVertical = -1f; // Bay xuống

                // 2. Tính toán góc nghiêng hợp lý
                if (moveMagnitude > 0.1f) 
                {
                    // TRƯỜNG HỢP A: Có di chuyển ngang (WASD)
                    if (flyVertical > 0) 
                        targetPitch = forwardPitch - 20f; // Bay ngang + Lên -> Hơi ngửa lên
                    else if (flyVertical < 0) 
                        targetPitch = forwardPitch + 20f; // Bay ngang + Xuống -> Hơi chúi xuống
                    else 
                        targetPitch = forwardPitch;       // Chỉ bay ngang (90 độ)
                }
                else 
                {
                    // TRƯỜNG HỢP B: Lơ lửng tại chỗ (Không bấm WASD)
                    if (flyVertical > 0) 
                        targetPitch = upPitch;            // Chỉ bay thẳng lên
                    else if (flyVertical < 0) 
                        targetPitch = downPitch;          // Chỉ bay thẳng xuống
                    else 
                        targetPitch = hoverPitch;         // Đứng im tại chỗ (0 độ)
                }
            }

            // 3. Thực thi nội suy (Slerp) để chuyển góc mượt mà
            Quaternion targetRotation = Quaternion.Euler(targetPitch, 0f, 0f);
            modelTransform.localRotation = Quaternion.Slerp(modelTransform.localRotation, targetRotation, Time.deltaTime * pitchSmoothSpeed);
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(flyToggleKey))
        {
            isFlying = !isFlying;
            ToggleThrusters(isFlying);

            Debug.Log(isFlying);

            if (isFlying)
            {
                controller.Move(Vector3.up * 1.5f);
                velocity.y = 5f; 
            }
        }
        

        float currentSpeed = isFlying ? flySpeed : walkSpeed;

        if (isAiming)
            HandleAimMovement(horizontal, vertical, currentSpeed);
        else
            HandleFreeMovement(horizontal, vertical, currentSpeed);

        // if (isFlying)
        // {
        //     float flyVertical = 0f;
        //     if (Input.GetKey(KeyCode.Space)) flyVertical = 1f;       // Bay lên
        //     if (Input.GetKey(KeyCode.LeftControl)) flyVertical = -1f; // Bay xuống

        //     velocity.y = flyVertical * ascendSpeed;
        // }
        // else
        // {
        //     if (controller.isGrounded && velocity.y < 0)
        //     {
        //         velocity.y = -2f;
        //     }
        //     velocity.y += gravity * Time.deltaTime;
        // }

        if (isFlying)
        {
            float flyVertical = 0f;
            if (Input.GetKey(KeyCode.Space)) flyVertical = 1f;       
            if (Input.GetKey(KeyCode.LeftControl)) flyVertical = -1f; 

            if (flyVertical != 0)
            {
                velocity.y = flyVertical * ascendSpeed;
            }
            else
            {
                velocity.y = Mathf.Lerp(velocity.y, 0f, Time.deltaTime * 3f);
            }
        }
        else
        {
            // Rơi tự do khi đi bộ
            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleFreeMovement(float horizontal, float vertical, float currentSpeed)
    {
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Góc đích = hướng phím WASD + hướng camera (camera-relative)
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            if (mainCamera != null) targetAngle += mainCamera.eulerAngles.y;

            // Làm mượt góc xoay kiểu critically-damped: mượt, không giật,
            // không vượt đích và độc lập framerate (mượt hơn hẳn Slerp).
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        float speedPercent = direction.magnitude == 0 ? 0f : (isFlying ? 1f : 0.5f);
        animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
    }

    private void HandleAimMovement(float horizontal, float vertical, float currentSpeed)
    {
        // 1. Chỉ ép thân nhân vật xoay trục Y (trái/phải) theo Camera. 
        // Trục X (lên/xuống) Camera đã tự lo bằng Pan Tilt.
        if (mainCamera != null)
        {
            transform.rotation = Quaternion.Euler(0f, mainCamera.eulerAngles.y, 0f);
        }

        // 2. WASD: Di chuyển ngang/dọc theo hướng mắt nhìn
        Vector3 moveDir = transform.right * horizontal + transform.forward * vertical;
        if (moveDir.magnitude > 0.1f)
        {
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        float speedPercent = moveDir.magnitude < 0.1f ? 0f : 0.5f;
        animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
    }
    
    // PlayerAimShooter gọi khi vào/ra chế độ ngắm.
    // Khi VÀO ngắm: quay nhân vật về đúng hướng camera đang nhìn -> AimCamera khớp liền mạch, không mất người.
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
        if (aiming && mainCamera != null)
            transform.rotation = Quaternion.Euler(0f, mainCamera.eulerAngles.y, 0f);
    }

    private void HandleCameraZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0f && cinemachineCamera != null)
        {
            // Xử lý zoom cho ThirdPersonFollow (Phổ biến nhất trong 3D)
            if (thirdPersonFollow != null)
            {
                thirdPersonFollow.CameraDistance -= scrollInput * zoomSpeed;
                thirdPersonFollow.CameraDistance = Mathf.Clamp(thirdPersonFollow.CameraDistance, minZoom, maxZoom);
            }
            // Xử lý zoom cho PositionComposer (thay thế cho FramingTransposer)
            else if (positionComposer != null)
            {
                positionComposer.CameraDistance -= scrollInput * zoomSpeed;
                positionComposer.CameraDistance = Mathf.Clamp(positionComposer.CameraDistance, minZoom, maxZoom);
            }
            // Xử lý zoom cho FreeLook / Orbital Follow (camera hiện tại của bạn)
            else if (orbitalFollow != null)
            {
                // RadialAxis.Value là HỆ SỐ NHÂN khoảng cách: nhỏ = gần, lớn = xa
                float v = orbitalFollow.RadialAxis.Value - scrollInput * orbitalZoomSpeed;
                orbitalFollow.RadialAxis.Value = Mathf.Clamp(v, minZoomMultiplier, maxZoomMultiplier);
            }
        }
    }

    public void ToggleFly()
    {
        isFlying = !isFlying;
        ToggleThrusters(isFlying);
    }

    private void ToggleThrusters(bool state)
    {
        if (leftThruster == null || rightThruster == null) return;

        if (state)
        {
            leftThruster.Play();
            rightThruster.Play();
        }
        else
        {
            leftThruster.Stop();
            rightThruster.Stop();
        }
    }
}