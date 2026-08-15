using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public sealed class AudioManager : MonoBehaviour
{
    public const string MasterVolumeParameter = "MasterVolume";
    public const string MusicVolumeParameter = "MusicVolume";
    public const string SfxVolumeParameter = "SfxVolume";
    public const string UIVolumeParameter = "UIVolume";
    public const string AmbienceVolumeParameter = "AmbienceVolume";

    private const string CoreSystemsResourceName = "CoreSystems";

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup ambienceGroup;

    [Header("Dedicated sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private AudioSource ambienceSource;

    [Header("Pooling")]
    [SerializeField, Min(1)] private int pooledSourceCount = 24;

    private AudioSourcePool sourcePool;
    private AudioSource activeMusicSource;
    private Coroutine musicFadeRoutine;

    public static AudioManager Instance { get; private set; }
    public AudioMixer Mixer => mixer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureCoreSystemsExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(CoreSystemsResourceName);
        if (prefab == null)
        {
            Debug.LogError("Resources/CoreSystems.prefab is missing. Run Tools/Audio/Build Audio Core.");
            return;
        }

        Object.Instantiate(prefab);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        sourcePool = new AudioSourcePool(transform, pooledSourceCount);
        activeMusicSource = musicSourceA;
        GameSettingsStorage.ApplyAudio(GameSettingsStorage.Load(), mixer);
    }

    private void Start()
    {
        // Unity can restore an AudioMixer snapshot after Awake. Reapply the saved
        // values in Start, before scene music controllers begin playback.
        GameSettingsStorage.ApplyAudio(GameSettingsStorage.Load(), mixer);
    }

    private void LateUpdate()
    {
        sourcePool?.UpdateFollowingSources();
    }

    public void PlayUI(AudioCue cue)
    {
        sourcePool?.Play(cue, uiGroup, Vector3.zero, null, true);
    }

    public void Play2D(AudioCue cue)
    {
        sourcePool?.Play(cue, ResolveGroup(cue), Vector3.zero, null, true);
    }

    public void PlayAt(AudioCue cue, Vector3 position)
    {
        sourcePool?.Play(cue, ResolveGroup(cue), position, null, false);
    }

    public AudioHandle PlayAtTracked(AudioCue cue, Vector3 position)
    {
        return sourcePool != null
            ? sourcePool.Play(cue, ResolveGroup(cue), position, null, false)
            : AudioHandle.Invalid;
    }

    public AudioHandle PlayLoop(AudioCue cue, Transform target)
    {
        if (cue == null || !cue.Loop)
        {
            Debug.LogWarning("PlayLoop requires an AudioCue with Loop enabled.", cue);
            return AudioHandle.Invalid;
        }

        Vector3 position = target != null ? target.position : Vector3.zero;
        return sourcePool != null
            ? sourcePool.Play(cue, ResolveGroup(cue), position, target, false)
            : AudioHandle.Invalid;
    }

    public void StopLoop(AudioHandle handle)
    {
        sourcePool?.Stop(handle);
    }

    public void Stop(AudioHandle handle)
    {
        sourcePool?.Stop(handle);
    }

    public void PlayMusic(AudioCue cue, float fadeDuration = 1f)
    {
        AudioClip clip = cue != null ? cue.GetRandomClip() : null;
        if (clip == null)
        {
            return;
        }

        AudioSource incoming = activeMusicSource == musicSourceA ? musicSourceB : musicSourceA;
        incoming.clip = clip;
        incoming.outputAudioMixerGroup = cue.OutputOverride != null ? cue.OutputOverride : musicGroup;
        incoming.pitch = cue.GetRandomPitch();
        incoming.loop = true;
        incoming.volume = 0f;
        incoming.Play();

        StartMusicFade(activeMusicSource, incoming, cue.GetRandomVolume(), fadeDuration);
        activeMusicSource = incoming;
    }

    public void StopMusic(float fadeDuration = 1f)
    {
        if (activeMusicSource == null || !activeMusicSource.isPlaying)
        {
            return;
        }

        StartMusicFade(activeMusicSource, null, 0f, fadeDuration);
    }

    public void PlayAmbience(AudioCue cue)
    {
        AudioClip clip = cue != null ? cue.GetRandomClip() : null;
        if (clip == null || ambienceSource == null)
        {
            return;
        }

        ambienceSource.clip = clip;
        ambienceSource.outputAudioMixerGroup = cue.OutputOverride != null
            ? cue.OutputOverride
            : ambienceGroup;
        ambienceSource.volume = cue.GetRandomVolume();
        ambienceSource.pitch = cue.GetRandomPitch();
        ambienceSource.loop = true;
        ambienceSource.Play();
    }

    public void StopAmbience()
    {
        if (ambienceSource != null)
        {
            ambienceSource.Stop();
        }
    }

    public bool SetBusVolume(string parameterName, float normalizedValue)
    {
        if (mixer == null)
        {
            return false;
        }

        float decibels = NormalizedToDecibels(normalizedValue);
        return mixer.SetFloat(parameterName, decibels);
    }

    public static float NormalizedToDecibels(float normalizedValue)
    {
        return normalizedValue <= 0.0001f
            ? -80f
            : Mathf.Log10(Mathf.Clamp01(normalizedValue)) * 20f;
    }

    private AudioMixerGroup ResolveGroup(AudioCue cue)
    {
        if (cue == null)
        {
            return sfxGroup;
        }

        return cue.Bus switch
        {
            AudioBus.UI => uiGroup,
            AudioBus.Ambience => ambienceGroup,
            _ => sfxGroup
        };
    }

    private void StartMusicFade(
        AudioSource outgoing,
        AudioSource incoming,
        float incomingTargetVolume,
        float duration
    )
    {
        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
        }

        musicFadeRoutine = StartCoroutine(
            CrossfadeMusic(outgoing, incoming, incomingTargetVolume, duration)
        );
    }

    private IEnumerator CrossfadeMusic(
        AudioSource outgoing,
        AudioSource incoming,
        float incomingTargetVolume,
        float duration
    )
    {
        float outgoingStartVolume = outgoing != null ? outgoing.volume : 0f;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            if (outgoing != null)
            {
                outgoing.volume = Mathf.Lerp(outgoingStartVolume, 0f, progress);
            }

            if (incoming != null)
            {
                incoming.volume = Mathf.Lerp(0f, incomingTargetVolume, progress);
            }

            yield return null;
        }

        if (outgoing != null)
        {
            outgoing.Stop();
            outgoing.clip = null;
        }

        if (incoming != null)
        {
            incoming.volume = incomingTargetVolume;
        }

        musicFadeRoutine = null;
    }
}
