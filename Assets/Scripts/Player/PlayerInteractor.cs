using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.I;

    [SerializeField, Min(0.5f)]
    private float interactionRadius = 3f;

    [SerializeField]
    private LayerMask interactionLayers = ~0;

    [Header("Prompt UI")]
    [SerializeField] private RectTransform promptContainer;
    [SerializeField] private InteractionPromptView promptPrefab;

    private readonly Collider[] nearbyColliders = new Collider[32];

    private readonly HashSet<IInteractable> detectedInteractables = new HashSet<IInteractable>();

    private readonly List<IInteractable> nearbyInteractables = new List<IInteractable>();

    private readonly Dictionary<IInteractable, InteractionPromptView> promptViews = new Dictionary<IInteractable, InteractionPromptView>();

    private readonly List<IInteractable> removalBuffer = new List<IInteractable>();

    private void Update()
    {
        if (RewardPopupUI.IsGameplayInputBlocked || InventoryMenuUI.IsOpen)
        {
            return;
        }
        
        DetectNearbyInteractables();
        SynchronizePrompts();
        UpdatePromptList();
        HandleInteraction();
    }

    private void DetectNearbyInteractables()
    {
        detectedInteractables.Clear();
        nearbyInteractables.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            interactionRadius,
            nearbyColliders,
            interactionLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = nearbyColliders[i];

            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            IInteractable interactable = FindInteractable(hit);

            if (!IsInteractableValid(interactable) || !interactable.CanInteract)
            {
                continue;
            }

            if (detectedInteractables.Add(interactable))
            {
                nearbyInteractables.Add(interactable);
            }
        }

        nearbyInteractables.Sort(CompareInteractableDistance);
    }

    private static IInteractable FindInteractable(Collider hit)
    {
        MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    private static bool IsInteractableValid(IInteractable interactable)
    {
        if (interactable == null)
        {
            return false;
        }

        UnityEngine.Object unityObject = interactable as UnityEngine.Object;

        return unityObject != null;
    }

    private int CompareInteractableDistance(IInteractable first, IInteractable second)
    {
        float firstDistance = (first.InteractionTransform.position - transform.position).sqrMagnitude;

        float secondDistance = (second.InteractionTransform.position - transform.position).sqrMagnitude;

        return firstDistance.CompareTo(secondDistance);
    }

    private void SynchronizePrompts()
    {
        if (promptContainer == null || promptPrefab == null)
        {
            return;
        }

        foreach (IInteractable interactable in nearbyInteractables)
        {
            if (promptViews.ContainsKey(interactable))
            {
                continue;
            }

            InteractionPromptView newPrompt = Instantiate(promptPrefab, promptContainer);

            newPrompt.gameObject.SetActive(true);
            promptViews.Add(interactable, newPrompt);
        }

        removalBuffer.Clear();

        foreach (KeyValuePair<IInteractable, InteractionPromptView> pair in promptViews)
        {
            if (!IsInteractableValid(pair.Key) || !detectedInteractables.Contains(pair.Key))
            {
                removalBuffer.Add(pair.Key);
            }
        }

        foreach (IInteractable interactable in removalBuffer)
        {
            RemovePrompt(interactable);
        }
    }

    private void UpdatePromptList()
    {
        for (int i = 0; i < nearbyInteractables.Count; i++)
        {
            IInteractable interactable = nearbyInteractables[i];

            if (!promptViews.TryGetValue(interactable, out InteractionPromptView prompt))
            {
                continue;
            }

            prompt.transform.SetSiblingIndex(i);
            bool isSelected = i == 0;
            string keyLabel = isSelected ? interactionKey.ToString().ToUpper() : string.Empty;
            prompt.SetContent(keyLabel, interactable.PromptText, isSelected);
        }
    }

    private void HandleInteraction()
    {
        if (nearbyInteractables.Count == 0)
        {
            return;
        }

        if (!Input.GetKeyDown(interactionKey))
        {
            return;
        }

        IInteractable selectedInteractable = nearbyInteractables[0];

        if (!IsInteractableValid(selectedInteractable) || !selectedInteractable.CanInteract)
        {
            return;
        }

        selectedInteractable.Interact();
        RemovePrompt(selectedInteractable);
        detectedInteractables.Remove(selectedInteractable);
        nearbyInteractables.RemoveAt(0);
    }

    private void RemovePrompt(IInteractable interactable)
    {
        if (!promptViews.TryGetValue(interactable, out InteractionPromptView prompt))
        {
            return;
        }

        promptViews.Remove(interactable);

        if (prompt != null)
        {
            Destroy(prompt.gameObject);
        }
    }

    private void OnDisable()
    {
        foreach (InteractionPromptView prompt in promptViews.Values)
        {
            if (prompt != null)
            {
                Destroy(prompt.gameObject);
            }
        }

        promptViews.Clear();
        detectedInteractables.Clear();
        nearbyInteractables.Clear();
        removalBuffer.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            interactionRadius);
    }
}