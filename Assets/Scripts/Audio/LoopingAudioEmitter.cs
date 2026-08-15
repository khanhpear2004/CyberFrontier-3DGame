using UnityEngine;

[DisallowMultipleComponent]
public sealed class LoopingAudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioCue loopCue;
    [SerializeField] private bool playOnEnable = true;

    private AudioHandle handle = AudioHandle.Invalid;

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        Stop();

        if (loopCue != null && AudioManager.Instance != null)
        {
            handle = AudioManager.Instance.PlayLoop(loopCue, transform);
        }
    }

    public void Stop()
    {
        if (handle.IsValid && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoop(handle);
        }

        handle = AudioHandle.Invalid;
    }
}
