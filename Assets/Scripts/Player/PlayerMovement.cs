using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public CharacterController controller;
    public float speed = 12f;

    [Header("Cài đặt trọng lực")]
    public float gravity = -9.81f;
    private Vector3 velocity;

    void Update()
    {
      
        float x = Input.GetAxis("Horizontal"); // A/D hoặc Trái/Phải
        float z = Input.GetAxis("Vertical");   // W/S hoặc Lên/Xuống
        Vector3 move = transform.right * x + transform.forward * z;

        // 3. Ra lệnh cho Character Controller di chuyển nhân vật
        controller.Move(move * speed * Time.deltaTime);

    
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}