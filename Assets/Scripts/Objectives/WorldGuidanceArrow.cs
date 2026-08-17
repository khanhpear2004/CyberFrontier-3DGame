using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class WorldGuidanceArrow : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("References")]
    [SerializeField] private ObjectiveBeacon objectiveBeacon;
    [SerializeField] private Transform player;

    [Header("Placement")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField, Min(0.5f)] private float distanceAhead = 4f;
    [SerializeField, Min(0.1f)] private float raycastHeight = 3f;
    [SerializeField, Min(0.1f)] private float raycastDistance = 8f;
    [SerializeField, Min(0f)] private float groundOffset = 0.05f;
    [SerializeField, Min(0f)] private float hideDistanceBelow = 3f;

    [Header("Appearance")]
    [SerializeField] private Color arrowColor = new Color(0f, 0.85f, 1f, 1f);
    [SerializeField] private Vector3 baseScale = Vector3.one;
    [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.08f;
    [SerializeField, Min(0f)] private float pulseSpeed = 1.5f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh runtimeMesh;
    private Material runtimeMaterial;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        CreateArrowVisual();
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            SetVisible(false);
        }
    }

    private void LateUpdate()
    {
        if (objectiveBeacon == null || player == null || !objectiveBeacon.HasGuidanceTarget || objectiveBeacon.RouteDistance <= hideDistanceBelow)
        {
            SetVisible(false);
            return;
        }

        Vector3 direction = objectiveBeacon.GuidancePosition - player.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            SetVisible(false);
            return;
        }

        Vector3 planarDirection = direction.normalized;
        float placementDistance = Mathf.Min(distanceAhead, direction.magnitude);
        Vector3 desiredPosition = player.position + planarDirection * placementDistance;
        Vector3 rayOrigin = desiredPosition + Vector3.up * raycastHeight;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            SetVisible(false);
            return;
        }

        Vector3 surfaceDirection = Vector3.ProjectOnPlane(planarDirection, groundHit.normal).normalized;

        if (surfaceDirection.sqrMagnitude <= 0.01f)
        {
            SetVisible(false);
            return;
        }

        transform.SetPositionAndRotation(groundHit.point + groundHit.normal * groundOffset, Quaternion.LookRotation(surfaceDirection, groundHit.normal));
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
        transform.localScale = baseScale * pulse;
        SetVisible(true);
    }

    private void OnDestroy()
    {
        if (runtimeMesh != null)
        {
            Destroy(runtimeMesh);
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    private void CreateArrowVisual()
    {
        runtimeMesh = new Mesh
        {
            name = "World Guidance Arrow Runtime Mesh"
        };

        runtimeMesh.vertices = new[]
        {
            new Vector3(0f, 0f, 1.2f),
            new Vector3(0.7f, 0f, 0.2f),
            new Vector3(0.25f, 0f, 0.2f),
            new Vector3(0.25f, 0f, -1f),
            new Vector3(-0.25f, 0f, -1f),
            new Vector3(-0.25f, 0f, 0.2f),
            new Vector3(-0.7f, 0f, 0.2f)
        };
        runtimeMesh.triangles = new[]
        {
            0, 1, 6,
            2, 3, 4,
            2, 4, 5
        };
        runtimeMesh.RecalculateNormals();
        runtimeMesh.RecalculateBounds();
        meshFilter.sharedMesh = runtimeMesh;

        Shader arrowShader = Shader.Find("Universal Render Pipeline/Unlit");

        if (arrowShader == null)
        {
            arrowShader = Shader.Find("Unlit/Color");
        }

        if (arrowShader == null)
        {
            Debug.LogError($"{name}: Missing an Unlit shader for the guidance arrow.", this);
            return;
        }

        runtimeMaterial = new Material(arrowShader)
        {
            name = "World Guidance Arrow Runtime Material"
        };

        if (runtimeMaterial.HasProperty(BaseColorId))
        {
            runtimeMaterial.SetColor(BaseColorId, arrowColor);
        }

        if (runtimeMaterial.HasProperty(ColorId))
        {
            runtimeMaterial.SetColor(ColorId, arrowColor);
        }

        meshRenderer.sharedMaterial = runtimeMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private void SetVisible(bool isVisible)
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = isVisible && runtimeMaterial != null;
        }
    }

    private bool HasRequiredReferences()
    {
        if (objectiveBeacon == null)
        {
            Debug.LogError($"{name}: Missing ObjectiveBeacon.", this);
            return false;
        }

        if (player == null)
        {
            Debug.LogError($"{name}: Missing Player Transform.", this);
            return false;
        }

        if (groundLayers.value == 0)
        {
            Debug.LogError($"{name}: Ground Layers cannot be empty.", this);
            return false;
        }

        return true;
    }
}
