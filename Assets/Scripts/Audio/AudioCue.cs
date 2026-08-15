using UnityEngine;
using UnityEngine.Audio;

public enum AudioBus
{
    Sfx,
    UI,
    Ambience
}

[CreateAssetMenu(fileName = "AudioCue", menuName = "Audio/Audio Cue")]
public sealed class AudioCue : ScriptableObject
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private AudioBus bus = AudioBus.Sfx;
    [SerializeField] private AudioMixerGroup outputOverride;
    [SerializeField, MinMaxRange(0f, 1f)] private Vector2 volumeRange = Vector2.one;
    [SerializeField, MinMaxRange(0.1f, 3f)] private Vector2 pitchRange = Vector2.one;
    [SerializeField] private bool loop;
    [SerializeField, Range(0f, 1f)] private float spatialBlend;
    [SerializeField, Min(0.01f)] private float minDistance = 1f;
    [SerializeField, Min(0.01f)] private float maxDistance = 30f;
    [SerializeField, Range(0, 256)] private int priority = 128;

    public AudioBus Bus => bus;
    public AudioMixerGroup OutputOverride => outputOverride;
    public bool Loop => loop;
    public float SpatialBlend => spatialBlend;
    public float MinDistance => minDistance;
    public float MaxDistance => maxDistance;
    public int Priority => priority;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        return clips[Random.Range(0, clips.Length)];
    }

    public float GetRandomVolume()
    {
        return Random.Range(volumeRange.x, volumeRange.y);
    }

    public float GetRandomPitch()
    {
        return Random.Range(pitchRange.x, pitchRange.y);
    }

    private void OnValidate()
    {
        float minimumVolume = Mathf.Min(volumeRange.x, volumeRange.y);
        float maximumVolume = Mathf.Max(volumeRange.x, volumeRange.y);
        float minimumPitch = Mathf.Min(pitchRange.x, pitchRange.y);
        float maximumPitch = Mathf.Max(pitchRange.x, pitchRange.y);
        volumeRange = new Vector2(Mathf.Clamp01(minimumVolume), Mathf.Clamp01(maximumVolume));
        pitchRange = new Vector2(
            Mathf.Clamp(minimumPitch, 0.1f, 3f),
            Mathf.Clamp(maximumPitch, 0.1f, 3f)
        );
        maxDistance = Mathf.Max(minDistance, maxDistance);
    }
}
