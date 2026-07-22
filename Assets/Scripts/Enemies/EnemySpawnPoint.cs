using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.forward * 1.5f
        );
    }
}