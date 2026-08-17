using TMPro;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class ObjectiveBeacon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform distanceOrigin;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform beaconRect;
    [SerializeField] private TMP_Text beaconText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Position")]
    [SerializeField, Min(0f)] private float screenPadding = 72f;
    [SerializeField] private float worldHeightOffset = 2f;

    [Header("Navigation")]
    [SerializeField] private bool useNavMeshRoute = true;
    [SerializeField, Min(0.1f)] private float routeRefreshInterval = 0.35f;
    [SerializeField, Min(0.1f)] private float cornerReachedDistance = 2.5f;
    [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 5f;

    [Header("Display")]
    [SerializeField, Min(0f)] private float hideDistanceBelow = 4f;
    [SerializeField] private string markerGlyph = "O";
    [SerializeField] private string distanceSuffix = "m";

    private Transform currentTarget;
    private NavMeshPath navigationPath;
    private Vector3 guidancePosition;
    private float routeDistance;
    private float nextRouteRefreshTime;
    private bool isOffScreen;

    public bool HasGuidanceTarget => currentTarget != null;
    public Vector3 GuidancePosition => guidancePosition;
    public float RouteDistance => routeDistance;

    private void Awake()
    {
        navigationPath = new NavMeshPath();
    }

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            SetVisible(false);
            return;
        }

        objectiveManager.ObjectiveChanged += HandleObjectiveChanged;
        objectiveManager.ObjectiveCompleted += HandleObjectiveCompleted;
        objectiveManager.ObjectiveCleared += HandleObjectiveCleared;
        RefreshTarget();
    }

    private void OnDisable()
    {
        if (objectiveManager != null)
        {
            objectiveManager.ObjectiveChanged -= HandleObjectiveChanged;
            objectiveManager.ObjectiveCompleted -= HandleObjectiveCompleted;
            objectiveManager.ObjectiveCleared -= HandleObjectiveCleared;
        }

        currentTarget = null;
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (currentTarget == null || objectiveManager.IsCurrentObjectiveCompleted)
        {
            SetVisible(false);
            return;
        }

        RefreshNavigationRouteIfNeeded();
        UpdateBeaconPosition();
        UpdateBeaconText();
        SetVisible(true);
    }

    private void HandleObjectiveChanged(ObjectiveState objective)
    {
        RefreshTarget();
    }

    private void HandleObjectiveCompleted(ObjectiveState objective)
    {
        RefreshTarget();
    }

    private void HandleObjectiveCleared()
    {
        currentTarget = null;
        SetVisible(false);
    }

    private void RefreshTarget()
    {
        if (!objectiveManager.HasActiveObjective || objectiveManager.IsCurrentObjectiveCompleted)
        {
            currentTarget = null;
            SetVisible(false);
            return;
        }

        currentTarget = objectiveManager.CurrentObjective.WorldTarget;
        nextRouteRefreshTime = 0f;
        guidancePosition = currentTarget != null ? currentTarget.position : Vector3.zero;
        SetVisible(currentTarget != null);
    }

    private void UpdateBeaconPosition()
    {
        Vector3 targetPosition = guidancePosition + Vector3.up * worldHeightOffset;
        Vector3 viewportPosition = targetCamera.WorldToViewportPoint(targetPosition);
        isOffScreen = viewportPosition.z <= 0f || viewportPosition.x < 0f || viewportPosition.x > 1f || viewportPosition.y < 0f || viewportPosition.y > 1f;

        if (viewportPosition.z < 0f)
        {
            viewportPosition.x = 1f - viewportPosition.x;
            viewportPosition.y = 1f - viewportPosition.y;
        }

        float horizontalPadding = Screen.width > 0 ? screenPadding / Screen.width : 0f;
        float verticalPadding = Screen.height > 0 ? screenPadding / Screen.height : 0f;
        viewportPosition.x = Mathf.Clamp(viewportPosition.x, horizontalPadding, 1f - horizontalPadding);
        viewportPosition.y = Mathf.Clamp(viewportPosition.y, verticalPadding, 1f - verticalPadding);

        Vector2 screenPosition = new Vector2(viewportPosition.x * Screen.width, viewportPosition.y * Screen.height);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPosition))
        {
            beaconRect.localPosition = localPosition;
        }
    }

    private void UpdateBeaconText()
    {
        string glyph = isOffScreen ? GetDirectionGlyph() : markerGlyph;
        beaconText.text = routeDistance <= hideDistanceBelow ? glyph : $"{glyph}\n{Mathf.CeilToInt(routeDistance)} {distanceSuffix}";
    }

    private void RefreshNavigationRouteIfNeeded()
    {
        if (Time.unscaledTime < nextRouteRefreshTime)
        {
            return;
        }

        nextRouteRefreshTime = Time.unscaledTime + routeRefreshInterval;
        Vector3 origin = distanceOrigin.position;
        Vector3 destination = currentTarget.position;

        if (!useNavMeshRoute || !TryCalculateRoute(origin, destination))
        {
            guidancePosition = destination;
            routeDistance = GetPlanarDistance(origin, destination);
        }
    }

    private bool TryCalculateRoute(Vector3 origin, Vector3 destination)
    {
        if (navigationPath == null)
        {
            navigationPath = new NavMeshPath();
        }

        if (!NavMesh.SamplePosition(origin, out NavMeshHit originHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            return false;
        }

        if (!NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            return false;
        }

        if (!NavMesh.CalculatePath(originHit.position, destinationHit.position, NavMesh.AllAreas, navigationPath) || navigationPath.status == NavMeshPathStatus.PathInvalid || navigationPath.corners.Length < 2)
        {
            return false;
        }

        Vector3[] corners = navigationPath.corners;
        int guidanceCornerIndex = FindGuidanceCornerIndex(origin, corners);
        guidancePosition = corners[guidanceCornerIndex];
        routeDistance = CalculateRouteDistance(origin, destination, corners);
        return true;
    }

    private int FindGuidanceCornerIndex(Vector3 origin, Vector3[] corners)
    {
        for (int index = 1; index < corners.Length; index++)
        {
            if (GetPlanarDistance(origin, corners[index]) > cornerReachedDistance)
            {
                return index;
            }
        }

        return corners.Length - 1;
    }

    private float CalculateRouteDistance(Vector3 origin, Vector3 destination, Vector3[] corners)
    {
        float distance = GetPlanarDistance(origin, corners[0]);

        for (int index = 1; index < corners.Length; index++)
        {
            distance += GetPlanarDistance(corners[index - 1], corners[index]);
        }

        distance += GetPlanarDistance(corners[^1], destination);
        return distance;
    }

    private float GetPlanarDistance(Vector3 firstPosition, Vector3 secondPosition)
    {
        Vector2 firstPlanarPosition = new Vector2(firstPosition.x, firstPosition.z);
        Vector2 secondPlanarPosition = new Vector2(secondPosition.x, secondPosition.z);
        return Vector2.Distance(firstPlanarPosition, secondPlanarPosition);
    }

    private string GetDirectionGlyph()
    {
        Vector3 targetScreenPosition = targetCamera.WorldToScreenPoint(guidancePosition + Vector3.up * worldHeightOffset);
        Vector2 direction = new Vector2(targetScreenPosition.x - Screen.width * 0.5f, targetScreenPosition.y - Screen.height * 0.5f);

        if (targetScreenPosition.z < 0f)
        {
            direction = -direction;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (angle >= -22.5f && angle < 22.5f)
        {
            return "E";
        }

        if (angle >= 22.5f && angle < 67.5f)
        {
            return "NE";
        }

        if (angle >= 67.5f && angle < 112.5f)
        {
            return "N";
        }

        if (angle >= 112.5f && angle < 157.5f)
        {
            return "NW";
        }

        if (angle >= 157.5f || angle < -157.5f)
        {
            return "W";
        }

        if (angle >= -157.5f && angle < -112.5f)
        {
            return "SW";
        }

        if (angle >= -112.5f && angle < -67.5f)
        {
            return "S";
        }

        return "SE";
    }

    private void SetVisible(bool isVisible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private bool HasRequiredReferences()
    {
        if (objectiveManager == null)
        {
            Debug.LogError($"{name}: Missing ObjectiveManager.", this);
            return false;
        }

        if (targetCamera == null)
        {
            Debug.LogError($"{name}: Missing Target Camera.", this);
            return false;
        }

        if (distanceOrigin == null)
        {
            Debug.LogError($"{name}: Missing Distance Origin.", this);
            return false;
        }

        if (canvasRect == null || beaconRect == null || beaconText == null || canvasGroup == null)
        {
            Debug.LogError($"{name}: Missing beacon UI references.", this);
            return false;
        }

        return true;
    }
}
