using UnityEngine;

public static class HitImpactUtility
{
    private const float SurfaceOffset = 0.01f;

    public static void Spawn(GameObject prefab, RaycastHit hit, float lifetime)
    {
        if (prefab == null)
        {
            return;
        }

        Vector3 normal = hit.normal.sqrMagnitude > 0.001f
            ? hit.normal.normalized
            : Vector3.up;

        Quaternion rotation = Quaternion.LookRotation(normal);
        Vector3 position = hit.point + normal * SurfaceOffset;

        GameObject instance = Object.Instantiate(prefab, position, rotation);
        Object.Destroy(instance, Mathf.Max(0.1f, lifetime));
    }
}
