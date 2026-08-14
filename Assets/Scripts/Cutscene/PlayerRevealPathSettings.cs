using UnityEngine;

public sealed class PlayerRevealPathSettings : MonoBehaviour
{
    [Header("Orbit Shape")]
    [SerializeField, Min(0.1f)] private float orbitRadius = 3.6f;
    [SerializeField, Min(0.1f)] private float closeUpRadius = 2.25f;
    [SerializeField] private float orbitBaseHeight = 1.2f;
    [SerializeField, Min(0f)] private float orbitHeightLift = 1f;

    public float OrbitRadius => orbitRadius;
    public float CloseUpRadius => closeUpRadius;
    public float OrbitBaseHeight => orbitBaseHeight;
    public float OrbitHeightLift => orbitHeightLift;
}
