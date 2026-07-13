using UnityEngine;

[DisallowMultipleComponent]
public class ChestInteractable : MonoBehaviour
{
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private string promptText = "Open Chest";
    [SerializeField] private bool canOpenOnlyOnce = true;

    private static readonly int OpenTrigger =
        Animator.StringToHash("Open");

    private bool isOpened;

    public string PromptText => promptText;

    public bool CanInteract => !canOpenOnlyOnce || !isOpened;

    private void Awake()
    {
        if (chestAnimator == null)
        {
            chestAnimator = GetComponentInChildren<Animator>();
        }
    }

    public void Interact()
    {
        if (!CanInteract || chestAnimator == null)
        {
            return;
        }

        isOpened = true;
        chestAnimator.SetTrigger(OpenTrigger);
    }
}