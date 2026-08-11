using UnityEngine;

[DisallowMultipleComponent]
public sealed class FinalGateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoundProgressManager roundProgressManager;
    [SerializeField] private Collider gateBlocker;
    [SerializeField] private GameObject gateVisual;
    [SerializeField] private Animator gateAnimator;

    [Header("Animation")]
    [SerializeField] private string openTriggerName = "Open";
    private bool isOpen;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        ApplyClosedState();
    }

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        roundProgressManager.AllEncountersCompleted += HandleAllEncountersCompleted;

        if (roundProgressManager.IsRoundCompleted)
        {
            OpenGate();
        }
    }

    private void OnDisable()
    {
        if (roundProgressManager != null)
        {
            roundProgressManager.AllEncountersCompleted -=
                HandleAllEncountersCompleted;
        }
    }

    private void HandleAllEncountersCompleted()
    {
        OpenGate();
    }

    public void OpenGate()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        gateBlocker.enabled = false;

        if (gateAnimator != null)
        {
            gateAnimator.SetTrigger(openTriggerName);
        }
        else if (gateVisual != null)
        {
            gateVisual.SetActive(false);
        }

        Debug.Log($"{name}: Final Gate đã mở.", this);
    }

    private void ApplyClosedState()
    {
        isOpen = false;

        if (gateBlocker != null)
        {
            gateBlocker.enabled = true;
        }

        if (gateVisual != null)
        {
            gateVisual.SetActive(true);
        }
    }

    private bool HasRequiredReferences()
    {
        if (roundProgressManager == null)
        {
            Debug.LogError($"{name}: Thiếu RoundProgressManager.", this);
            return false;
        }

        if (gateBlocker == null)
        {
            Debug.LogError($"{name}: Thiếu Gate Blocker.", this);
            return false;
        }

        return true;
    }
}