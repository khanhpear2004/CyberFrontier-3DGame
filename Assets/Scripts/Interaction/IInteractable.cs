using UnityEngine;

public interface IInteractable
{
    string PromptText { get; }

    bool CanInteract { get; }

    Transform InteractionTransform { get; }

    void Interact();
}