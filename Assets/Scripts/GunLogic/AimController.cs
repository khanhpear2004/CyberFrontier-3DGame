using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Chỉ số súng")]
    public float damage = 10f; // Sát thương
    public float range = 100f; // Tầm bắn

    [Header("Tham chiếu")]
    public Camera fpsCam; // Camera của người chơi

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        RaycastHit hit;
        
        // Bắn tia Raycast từ vị trí camera, hướng thẳng về phía trước (trục Z của camera)
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, range))
        {
            Debug.Log("Đã bắn trúng: " + hit.transform.name);
        }
    }
}