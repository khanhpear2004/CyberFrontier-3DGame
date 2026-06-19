using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float flySpeed = 5.0f;
    public float rotationSpeed = 10.0f;
    public float gravity = -9.81f;

    [Header("Fly Toggle (bật/tắt bay lơ lửng)")]
    public KeyCode flyToggleKey = KeyCode.LeftShift;
    private bool isFlying = false;

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

    private CinemachineThirdPersonFollow thirdPersonFollow;
    private CinemachinePositionComposer positionComposer;
    private CinemachineOrbitalFollow orbitalFollow;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

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
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (Input.GetKeyDown(flyToggleKey))
        {
            isFlying = !isFlying;
            ToggleThrusters(isFlying);
        }

        float currentSpeed = isFlying ? flySpeed : walkSpeed;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            if (mainCamera != null)
            {
                targetAngle += mainCamera.eulerAngles.y;
            }

            Quaternion rotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        float speedPercent = direction.magnitude == 0 ? 0f : (isFlying ? 1f : 0.5f);
        animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
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