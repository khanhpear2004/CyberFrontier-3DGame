using UnityEngine;

public sealed class MainMenuAudioController : MonoBehaviour
{
    [SerializeField] private AudioCue ambienceCue;
    [SerializeField] private AudioCue musicCue;
    [SerializeField, Min(0.01f)] private float musicFadeDuration = 1.5f;

    private bool startedAmbience;
    private bool startedMusic;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        if (ambienceCue != null)
        {
            AudioManager.Instance.PlayAmbience(ambienceCue);
            startedAmbience = true;
        }

        if (musicCue != null)
        {
            AudioManager.Instance.PlayMusic(musicCue, musicFadeDuration);
            startedMusic = true;
        }
    }

    private void OnDestroy()
    {
        if (startedAmbience && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopAmbience();
        }

        if (startedMusic && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic(musicFadeDuration);
        }
    }
}
