using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField, Min(0.5f)] private float interactionRadius = 2.5f;
    [SerializeField] private LayerMask interactionLayers = ~0;

    [Header("UI")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private TMP_Text actionText;

    private readonly Collider[] nearbyColliders = new Collider[32];
    private ChestInteractable currentChest;

    private void Awake()
    {
        if (keyText != null)
        {
            keyText.text = interactionKey.ToString().ToUpper();
        }

        SetPromptVisible(false);
    }

    private void Update()
    {
        currentChest = FindNearestChest();

        if (currentChest == null)
        {
            SetPromptVisible(false);
            return;
        }

        SetPromptVisible(true);

        if (actionText != null)
        {
            actionText.text = currentChest.PromptText;
        }

        if (Input.GetKeyDown(interactionKey))
        {
            currentChest.Interact();

            if (!currentChest.CanInteract)
            {
                SetPromptVisible(false);
            }
        }
    }

    private ChestInteractable FindNearestChest()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            interactionRadius,
            nearbyColliders,
            interactionLayers,
            QueryTriggerInteraction.Collide);

        ChestInteractable nearestChest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = nearbyColliders[i];

            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            ChestInteractable chest =
                hit.GetComponentInParent<ChestInteractable>();

            if (chest == null || !chest.CanInteract)
            {
                continue;
            }

            Vector3 nearestPoint = hit.ClosestPoint(transform.position);
            float distance =
                (nearestPoint - transform.position).sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestChest = chest;
            }
        }

        return nearestChest;
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (promptPanel != null &&
            promptPanel.activeSelf != isVisible)
        {
            promptPanel.SetActive(isVisible);
        }
    }

    private void OnDisable()
    {
        SetPromptVisible(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}