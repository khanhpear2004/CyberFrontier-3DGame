using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    public float rotationSpeed = 10.0f;
    public float gravity = -9.81f;

    // THÊM MỚI: Biến lưu trữ Camera
    [Header("Camera Info")]
    public Transform mainCamera; 

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Tự động tìm Main Camera nếu bạn quên gán ngoài Scene
        if (mainCamera == null) 
        {
            mainCamera = Camera.main.transform;
        }

        // Tùy chọn: Khóa con trỏ chuột vào giữa màn hình để xoay camera mượt hơn
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        if (direction.magnitude >= 0.1f)
        {
            // THAY ĐỔI QUAN TRỌNG: Cộng thêm góc xoay của Camera (mainCamera.eulerAngles.y)
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
            
            // Xoay nhân vật
            Quaternion rotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

            // Di chuyển nhân vật
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
        }

        float speedPercent = direction.magnitude == 0 ? 0f : (isRunning ? 1f : 0.5f);
        animator.SetFloat("Speed", speedPercent, 0.1f, Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}