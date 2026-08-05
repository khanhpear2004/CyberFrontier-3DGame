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

    private readonly Collider[] nearbyColliders =
        new Collider[32];

    // Ngăn một rương bị thêm nhiều lần nếu có nhiều collider.
    private readonly HashSet<ChestInteractable> detectedChests =
        new HashSet<ChestInteractable>();

    // Danh sách được sắp xếp từ gần đến xa.
    private readonly List<ChestInteractable> nearbyChests =
        new List<ChestInteractable>();

    // Ghép mỗi rương với đúng prompt của nó.
    private readonly Dictionary<
        ChestInteractable,
        InteractionPromptView> promptViews =
        new Dictionary<
            ChestInteractable,
            InteractionPromptView>();

    // Danh sách tạm để xóa prompt an toàn.
    private readonly List<ChestInteractable> removalBuffer =
        new List<ChestInteractable>();

    private void Update()
    {
        if (RewardPopupUI.IsGameplayInputBlocked || InventoryMenuUI.IsOpen)
        {
            return;
        }
        
        DetectNearbyChests();
        SynchronizePrompts();
        UpdatePromptList();
        HandleInteraction();
    }

    private void DetectNearbyChests()
    {
        detectedChests.Clear();
        nearbyChests.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            interactionRadius,
            nearbyColliders,
            interactionLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = nearbyColliders[i];

            if (hit == null ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            ChestInteractable chest =
                hit.GetComponentInParent<ChestInteractable>();

            if (chest == null || !chest.CanInteract)
            {
                continue;
            }

            // HashSet.Add trả về true nếu đây là rương mới.
            if (detectedChests.Add(chest))
            {
                nearbyChests.Add(chest);
            }
        }

        // Rương gần nhất sẽ nằm ở index 0.
        nearbyChests.Sort(CompareChestDistance);
    }

    private int CompareChestDistance(
        ChestInteractable first,
        ChestInteractable second)
    {
        float firstDistance =
            (first.transform.position - transform.position)
            .sqrMagnitude;

        float secondDistance =
            (second.transform.position - transform.position)
            .sqrMagnitude;

        return firstDistance.CompareTo(secondDistance);
    }

    private void SynchronizePrompts()
    {
        if (promptContainer == null || promptPrefab == null)
        {
            return;
        }

        // Tạo prompt cho những rương mới đi vào phạm vi.
        foreach (ChestInteractable chest in nearbyChests)
        {
            if (promptViews.ContainsKey(chest))
            {
                continue;
            }

            InteractionPromptView newPrompt =
                Instantiate(promptPrefab, promptContainer);

            newPrompt.gameObject.SetActive(true);
            promptViews.Add(chest, newPrompt);
        }

        // Tìm prompt của những rương đã ra khỏi phạm vi.
        removalBuffer.Clear();

        foreach (
            KeyValuePair<
                ChestInteractable,
                InteractionPromptView> pair in promptViews)
        {
            if (pair.Key == null ||
                !detectedChests.Contains(pair.Key))
            {
                removalBuffer.Add(pair.Key);
            }
        }

        // Xóa sau khi đã duyệt Dictionary xong.
        foreach (ChestInteractable chest in removalBuffer)
        {
            RemovePrompt(chest);
        }
    }

    private void UpdatePromptList()
    {
        for (int i = 0; i < nearbyChests.Count; i++)
        {
            ChestInteractable chest = nearbyChests[i];

            if (!promptViews.TryGetValue(
                    chest,
                    out InteractionPromptView prompt))
            {
                continue;
            }

           
            prompt.transform.SetSiblingIndex(i);
            bool isSelected = i == 0;
            string keyLabel = isSelected
                ? interactionKey.ToString().ToUpper()
                : string.Empty;

            prompt.SetContent(
                keyLabel,
                chest.PromptText,
                isSelected);
        }
    }

    private void HandleInteraction()
    {
        if (nearbyChests.Count == 0)
        {
            return;
        }

        if (!Input.GetKeyDown(interactionKey))
        {
            return;
        }

        ChestInteractable selectedChest = nearbyChests[0];

        selectedChest.Interact();
        RemovePrompt(selectedChest);

        detectedChests.Remove(selectedChest);
        nearbyChests.RemoveAt(0);
    }

    private void RemovePrompt(ChestInteractable chest)
    {
        if (!promptViews.TryGetValue(
                chest,
                out InteractionPromptView prompt))
        {
            return;
        }

        promptViews.Remove(chest);

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
        detectedChests.Clear();
        nearbyChests.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            interactionRadius);
    }
}