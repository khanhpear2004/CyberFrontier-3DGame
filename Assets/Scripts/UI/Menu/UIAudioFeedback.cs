using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIAudioFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler,
    ISelectHandler, ISubmitHandler
{
    [SerializeField] private AudioCue hoverCue;
    [SerializeField] private AudioCue clickCue;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Play(hoverCue);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Play(clickCue);
    }

    public void OnSelect(BaseEventData eventData)
    {
        Play(hoverCue);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Play(clickCue);
    }

    private static void Play(AudioCue cue)
    {
        if (cue != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(cue);
        }
    }
}
