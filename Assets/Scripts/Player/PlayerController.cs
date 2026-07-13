using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float flySpeed = 5.0f;
    public float runSpeed = 6.0f;
    public float flyVerticalInput = 0f;
    public KeyCode runKey = KeyCode.LeftShift;

    [Tooltip("Thời gian làm mượt khi xoay (giây). Nhỏ = xoay nhanh & gắt, lớn = mượt & chậm. ~0.1 là mượt mà nhạy.")]
    public float turnSmoothTime = 0.1f;
    public float gravity = -9.81f;

    [Header("Aim Mode (chế độ ngắm bắn)")]
    public float aimTurnSpeed = 200f;                // Độ nhạy xoay nhân vật bằng chuột khi ngắm
    [HideInInspector] public bool isAiming = false;  // PlayerAimShooter bật/tắt qua SetAiming()

    [Header("Fly Toggle (bật/tắt bay lơ lửng)")]
    public KeyCode flyToggleKey;
    public bool isFlying = false;
    public bool isRunning = false;

    [Header("Flight Controls (Lớp Con & Lực đẩy)")]
    public Transform modelTransform; 
    public KeyCode flyUpKey = KeyCode.Space;      
    public KeyCode flyDownKey = KeyCode.LeftControl;
    public float ascendSpeed = 4.0f;

    [Header("Flight Angles (Chỉnh góc nghiêng X khi bay)")]
    public float hoverPitch = -90f;        
    public float forwardPitch = 90f;    
    public float upPitch = -30f;  
    public float downPitch = 120f; 
    public float pitchSmoothSpeed = 4f;

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
    private float turnSmoothVelocity;

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
           
            cinemachineCamera.TryGetComponent(out thirdPersonFollow);
            cinemachineCamera.TryGetComponent(out positionComposer);
            cinemachineCamera.TryGetComponent(out orbitalFollow);

          
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
        flyVerticalInput = 0f;
        if (isFlying)
        {
            if (Input.GetMouseButton(0)) flyVerticalInput = 1f;
            if (Input.GetMouseButton(1)) flyVerticalInput = -1f;
        }

        HandleMovement();
        HandleCameraZoom();
        HandleFlying();
    }

    private void HandleFlying()
    {
        if (modelTransform != null)
        {
            float targetPitch = 0f; 

            if (animator != null)
            {
                animator.SetBool("isFlying", isFlying);
            }

            if (isFlying)
            {
                float moveMagnitude = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        
                if (moveMagnitude > 0.1f) 
                {
                    if (flyVerticalInput > 0) 
                        targetPitch = forwardPitch - 20f; 
                    else if (flyVerticalInput < 0) 
                        targetPitch = forwardPitch + 20f; 
                    else 
                        targetPitch = forwardPitch;
                }
                else 
                {
                    if (flyVerticalInput > 0) 
                        targetPitch = upPitch;            
                    else if (flyVerticalInput < 0) 
                        targetPitch = downPitch;  
                    else 
                        targetPitch = hoverPitch;   
                }
            }
            Quaternion targetRotation = Quaternion.Euler(targetPitch, 0f, 0f);
            modelTransform.localRotation = Quaternion.Slerp(modelTransform.localRotation, targetRotation, Time.deltaTime * pitchSmoothSpeed);
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        isRunning = Input.GetKey(runKey);

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
        
        float currentSpeed;
        if (isFlying) 
            currentSpeed = flySpeed;
        else if (isRunning) 
            currentSpeed = runSpeed;
        else 
            currentSpeed = walkSpeed;

        if (isAiming)
            HandleAimMovement(horizontal, vertical, currentSpeed);
        else
            HandleFreeMovement(horizontal, vertical, currentSpeed, isRunning);

        if (isFlying)
        {
            if (flyVerticalInput != 0)
            {
                velocity.y = flyVerticalInput * ascendSpeed;
            }
            else
            {
                velocity.y = Mathf.Lerp(velocity.y, 0f, Time.deltaTime * 3f);
            }
        }
        else
        {
            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleFreeMovement(float horizontal, float vertical, float currentSpeed, bool isRunning)
    {
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
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

        float speedPercent = 0f;
        if (direction.magnitude > 0.1f)
        {
            if (isFlying || isRunning) 
                speedPercent = 1f;    
            else 
                speedPercent = 0.5f;  
        }
        animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);
    }

    private void HandleAimMovement(float horizontal, float vertical, float currentSpeed)
    {
        if (mainCamera != null)
        {
            transform.rotation = Quaternion.Euler(0f, mainCamera.eulerAngles.y, 0f);
        }

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