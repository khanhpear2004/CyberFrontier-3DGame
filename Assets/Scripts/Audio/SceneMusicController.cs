using UnityEngine;

public sealed class SceneMusicController : MonoBehaviour
{
    [SerializeField] private AudioCue musicCue;
    [SerializeField, Min(0.01f)] private float fadeDuration = 1.5f;

    private bool startedMusic;

    private void Start()
    {
        if (musicCue == null || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlayMusic(musicCue, fadeDuration);
        startedMusic = true;
    }

    private void OnDestroy()
    {
        if (startedMusic && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic(fadeDuration);
        }
    }
}
