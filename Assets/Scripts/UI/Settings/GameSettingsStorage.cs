using UnityEngine;
using UnityEngine.Audio;

public readonly struct GameSettingsData
{
    public GameSettingsData(float masterVolume, float musicVolume, float sfxVolume, float brightness,
        int resolutionWidth, int resolutionHeight, bool fullscreen, int qualityLevel)
    {
        MasterVolume = Mathf.Clamp01(masterVolume);
        MusicVolume = Mathf.Clamp01(musicVolume);
        SfxVolume = Mathf.Clamp01(sfxVolume);
        Brightness = Mathf.Clamp01(brightness);
        ResolutionWidth = Mathf.Max(640, resolutionWidth);
        ResolutionHeight = Mathf.Max(360, resolutionHeight);
        Fullscreen = fullscreen;
        QualityLevel = Mathf.Max(0, qualityLevel);
    }

    public float MasterVolume { get; }
    public float MusicVolume { get; }
    public float SfxVolume { get; }
    public int ResolutionWidth { get; }
    public int ResolutionHeight { get; }
    public bool Fullscreen { get; }
    public int QualityLevel { get; }
    public float Brightness { get; }
}

public static class GameSettingsStorage
{
    private const string SavedKey = "Settings.Saved";
    private const string MasterVolumeKey = "Settings.Audio.Master";
    private const string MusicVolumeKey = "Settings.Audio.Music";
    private const string SfxVolumeKey = "Settings.Audio.Sfx";
    private const string ResolutionWidthKey = "Settings.Display.Width";
    private const string ResolutionHeightKey = "Settings.Display.Height";
    private const string FullscreenKey = "Settings.Display.Fullscreen";
    private const string QualityLevelKey = "Settings.Display.Quality";

    private const string BrightnessKey = "Settings.Display.Brightness";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedDisplaySettings()
    {
        ApplyDisplay(Load());
    }

    public static GameSettingsData Load()
    {
        GameSettingsData defaults = CreateDefaults();
        if (PlayerPrefs.GetInt(SavedKey, 0) == 0)
        {
            return defaults;
        }

        return new GameSettingsData(
            PlayerPrefs.GetFloat(MasterVolumeKey, defaults.MasterVolume),
            PlayerPrefs.GetFloat(MusicVolumeKey, defaults.MusicVolume),
            PlayerPrefs.GetFloat(SfxVolumeKey, defaults.SfxVolume),
            PlayerPrefs.GetFloat(BrightnessKey, defaults.Brightness),
            PlayerPrefs.GetInt(ResolutionWidthKey, defaults.ResolutionWidth),
            PlayerPrefs.GetInt(ResolutionHeightKey, defaults.ResolutionHeight),
            PlayerPrefs.GetInt(FullscreenKey, defaults.Fullscreen ? 1 : 0) == 1,
            ClampQualityLevel(PlayerPrefs.GetInt(QualityLevelKey, defaults.QualityLevel))
        );
    }

    public static GameSettingsData CreateDefaults()
    {
        Resolution nativeResolution = Screen.currentResolution;
        return new GameSettingsData(1f, 0.8f, 1f, 0.5f, nativeResolution.width,
            nativeResolution.height, true, Mathf.Max(0, QualitySettings.names.Length - 1));
    }

    public static void Save(GameSettingsData settings)
    {
        PlayerPrefs.SetInt(SavedKey, 1);
        PlayerPrefs.SetFloat(MasterVolumeKey, settings.MasterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, settings.MusicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, settings.SfxVolume);
        PlayerPrefs.SetFloat(BrightnessKey, settings.Brightness);
        PlayerPrefs.SetInt(ResolutionWidthKey, settings.ResolutionWidth);
        PlayerPrefs.SetInt(ResolutionHeightKey, settings.ResolutionHeight);
        PlayerPrefs.SetInt(FullscreenKey, settings.Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(QualityLevelKey, ClampQualityLevel(settings.QualityLevel));
        PlayerPrefs.Save();
    }

    public static void ApplyAudio(GameSettingsData settings, AudioMixer mixer)
    {
        if (mixer == null)
        {
            return;
        }

        mixer.SetFloat(AudioManager.MasterVolumeParameter, AudioManager.NormalizedToDecibels(settings.MasterVolume));

        mixer.SetFloat(AudioManager.MusicVolumeParameter, AudioManager.NormalizedToDecibels(settings.MusicVolume));

        float sfxDecibels = AudioManager.NormalizedToDecibels(settings.SfxVolume);

        mixer.SetFloat(AudioManager.SfxVolumeParameter, sfxDecibels);

        mixer.SetFloat(AudioManager.UIVolumeParameter, sfxDecibels);
    }

    public static void ApplyDisplay(GameSettingsData settings)
    {
        QualitySettings.SetQualityLevel(ClampQualityLevel(settings.QualityLevel), true);
        FullScreenMode mode = settings.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(settings.ResolutionWidth, settings.ResolutionHeight, mode);
    }

    private static int ClampQualityLevel(int qualityLevel)
    {
        return Mathf.Clamp(qualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
    }
}
