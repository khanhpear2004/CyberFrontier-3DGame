using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float flySpeed = 5.0f;
    public float runSpeed = 6.0f;
    [SerializeField] private RuntimeStats runtimeStats;
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

    [Header("Movement Smoothing")]
    [SerializeField, Min(0.01f)] private float inputSmoothTime = 0.08f;
    [SerializeField, Min(0f)] private float acceleration = 14f;
    [SerializeField, Min(0f)] private float deceleration = 18f;

    private Vector2 smoothedInput;
    private Vector2 inputSmoothVelocity;
    private float currentMoveSpeed;

    private Vector3 currentPlanarVelocity;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private static readonly int AimHorizontalHash = Animator.StringToHash("AimHorizontal");

    private static readonly int AimVerticalHash = Animator.StringToHash("AimVertical");

    private void Awake()
    {
        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<RuntimeStats>();
        }
    }

    private void OnEnable()
    {
        if (runtimeStats != null)
        {
            runtimeStats.StatChanged += HandleStatChanged;
        }
    }

    private void OnDisable()
    {
        if (runtimeStats != null)
        {
            runtimeStats.StatChanged -= HandleStatChanged;
        }
    }

    void Start()
    {
        RefreshMovementStats();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }

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

    private void RefreshMovementStats()
    {
        if (runtimeStats == null)
        {
            return;
        }

        walkSpeed = Mathf.Max(
            0f,
            runtimeStats.GetValue(StatType.WalkSpeed)
        );

        runSpeed = Mathf.Max(
            0f,
            runtimeStats.GetValue(StatType.RunSpeed)
        );

        flySpeed = Mathf.Max(
            0f,
            runtimeStats.GetValue(StatType.FlySpeed)
        );
    }

    private void HandleStatChanged(
        StatType statType,
        float oldValue,
        float newValue
    )
    {
        float safeValue = Mathf.Max(0f, newValue);

        switch (statType)
        {
            case StatType.WalkSpeed: walkSpeed = safeValue; break;

            case StatType.RunSpeed: runSpeed = safeValue; break;

            case StatType.FlySpeed: flySpeed = safeValue; break;
        }
    }

    void Update()
    {
        if (RewardPopupUI.IsGameplayInputBlocked || InventoryMenuUI.IsOpen)
        {
            return;
        }
        
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
        Vector2 rawInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        rawInput = Vector2.ClampMagnitude(rawInput, 1f);

        smoothedInput = Vector2.SmoothDamp(smoothedInput, rawInput, ref inputSmoothVelocity, inputSmoothTime);

        UpdateAimLocomotionAnimation(smoothedInput);

        float inputAmount = Mathf.Clamp01(smoothedInput.magnitude);

        if (Input.GetKeyDown(runKey))
        {
            isRunning = !isRunning;
        }

        HandleFlyToggle();

        float maximumSpeed;

        if (isFlying)
        {
            maximumSpeed = flySpeed;
        }
        else if (isRunning)
        {
            maximumSpeed = runSpeed;
        }
        else
        {
            maximumSpeed = walkSpeed;
        }

        Vector3 moveDirection = GetCameraRelativeDirection(smoothedInput.x, smoothedInput.y);

        Vector3 targetPlanarVelocity = moveDirection * maximumSpeed;

        float changeRate = inputAmount > 0.01f ? acceleration : deceleration;

        currentPlanarVelocity = Vector3.MoveTowards(currentPlanarVelocity, targetPlanarVelocity, changeRate * Time.deltaTime);

        UpdateCharacterRotation();
        UpdateVerticalVelocity();

        Vector3 finalVelocity =currentPlanarVelocity + Vector3.up * velocity.y;

        controller.Move( finalVelocity * Time.deltaTime);

        currentMoveSpeed = currentPlanarVelocity.magnitude;

        UpdateLocomotionAnimation();
    }

    private Vector3 GetCameraRelativeDirection(float horizontal, float vertical)
    {
        Transform directionReference = mainCamera != null ? mainCamera : transform;

        Vector3 forward = directionReference.forward;
        Vector3 right = directionReference.right;

        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = transform.forward;
        }

        forward.Normalize();
        right.Normalize();

        Vector3 direction =forward * vertical + right * horizontal;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    private void HandleFlyToggle()
    {
        if (!Input.GetKeyDown(flyToggleKey))
        {
            return;
        }

        isFlying = !isFlying;
        ToggleThrusters(isFlying);

        if (isFlying)
        {
            velocity.y = ascendSpeed;
        }
    }

    private void UpdateCharacterRotation()
    {
        bool hasRotationTarget = false;
        float targetAngle = transform.eulerAngles.y;

        if (isAiming && mainCamera != null)
        {
            targetAngle = mainCamera.eulerAngles.y;
            hasRotationTarget = true;
        }
        else if (currentPlanarVelocity.sqrMagnitude > 0.01f)
        {
            targetAngle = Mathf.Atan2( currentPlanarVelocity.x, currentPlanarVelocity.z) * Mathf.Rad2Deg;

            hasRotationTarget = true;
        }

        if (!hasRotationTarget)
        {
            return;
        }

        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
    }
    private void UpdateLocomotionAnimation()
    {
        if (animator == null)
        {
            return;
        }

        float animationSpeed;

        if (currentMoveSpeed <= walkSpeed)
        {
            animationSpeed =0.5f * Mathf.InverseLerp( 0f, walkSpeed, currentMoveSpeed);
        }
        else
        {
            animationSpeed =Mathf.Lerp( 0.5f, 1f, Mathf.InverseLerp( walkSpeed, runSpeed, currentMoveSpeed));
        }

        animator.SetFloat(SpeedHash, animationSpeed, 0.12f, Time.deltaTime);
    }

    private void UpdateVerticalVelocity()
    {
        if (isFlying)
        {
            if (Mathf.Abs(flyVerticalInput) > 0.01f)
            {
                velocity.y = flyVerticalInput * ascendSpeed;
            }
            else
            {
                velocity.y = Mathf.Lerp( velocity.y, 0f, Time.deltaTime * 3f);
            }
            return;
        }

        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
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

    private void UpdateAimLocomotionAnimation(Vector2 input)
    {
        if (animator == null)
        {
            return;
        }

        float horizontal = isAiming ? input.x : 0f;
        float vertical = isAiming ? input.y : 0f;

        animator.SetFloat(AimHorizontalHash, horizontal, 0.08f, Time.deltaTime);

        animator.SetFloat(AimVerticalHash, vertical, 0.08f, Time.deltaTime);
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
