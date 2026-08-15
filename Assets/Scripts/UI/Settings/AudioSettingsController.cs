using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class AudioSettingsController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider brightnessSlider;

    [Header("Display")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Actions")]
    [SerializeField] private Button defaultButton;
    [SerializeField] private Button applyButton;

    private readonly List<Vector2Int> availableResolutions = new();
    private GameSettingsData committedSettings;
    private bool listenersAdded;

    private void Awake()
    {
        ResolveMissingReferences();
        BuildResolutionOptions();
        BuildQualityOptions();

        committedSettings = GameSettingsStorage.Load();
        GameSettingsStorage.ApplyAudio(committedSettings, mixer);
        WriteSettingsToUI(committedSettings);
        SetBrightness(committedSettings.Brightness);
    }

    private void OnEnable()
    {
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void BeginEdit()
    {
        committedSettings = GameSettingsStorage.Load();
        WriteSettingsToUI(committedSettings);
        GameSettingsStorage.ApplyAudio(committedSettings, mixer);
        SetBrightness(committedSettings.Brightness);
    }

    public void CancelPendingChanges()
    {
        WriteSettingsToUI(committedSettings);
        GameSettingsStorage.ApplyAudio(committedSettings, mixer);
        SetBrightness(committedSettings.Brightness);
    }

    private void AddListeners()
    {
        if (listenersAdded)
        {
            return;
        }

        masterVolumeSlider?.onValueChanged.AddListener(SetMasterVolume);
        musicVolumeSlider?.onValueChanged.AddListener(SetMusicVolume);
        sfxVolumeSlider?.onValueChanged.AddListener(SetSfxVolume);
        brightnessSlider?.onValueChanged.AddListener(SetBrightness);
        defaultButton?.onClick.AddListener(UseDefaults);
        applyButton?.onClick.AddListener(ApplyAndSave);
        listenersAdded = true;
    }

    private void RemoveListeners()
    {
        if (!listenersAdded)
        {
            return;
        }

        masterVolumeSlider?.onValueChanged.RemoveListener(SetMasterVolume);
        musicVolumeSlider?.onValueChanged.RemoveListener(SetMusicVolume);
        sfxVolumeSlider?.onValueChanged.RemoveListener(SetSfxVolume);
        brightnessSlider?.onValueChanged.RemoveListener(SetBrightness);
        defaultButton?.onClick.RemoveListener(UseDefaults);
        applyButton?.onClick.RemoveListener(ApplyAndSave);
        listenersAdded = false;
    }

    private void ApplyAndSave()
    {
        committedSettings = ReadSettingsFromUI();
        GameSettingsStorage.ApplyAudio(committedSettings, mixer);
        GameSettingsStorage.ApplyDisplay(committedSettings);
        SetBrightness(committedSettings.Brightness);
        GameSettingsStorage.Save(committedSettings);
        WriteSettingsToUI(committedSettings);
    }

    private void UseDefaults()
    {
        GameSettingsData defaults = GameSettingsStorage.CreateDefaults();
        WriteSettingsToUI(defaults);
        GameSettingsStorage.ApplyAudio(defaults, mixer);
        SetBrightness(defaults.Brightness);
    }

    private GameSettingsData ReadSettingsFromUI()
    {
        Vector2Int resolution = GetSelectedResolution();
        return new GameSettingsData(
            masterVolumeSlider != null ? masterVolumeSlider.value : committedSettings.MasterVolume,
            musicVolumeSlider != null ? musicVolumeSlider.value : committedSettings.MusicVolume,
            sfxVolumeSlider != null ? sfxVolumeSlider.value : committedSettings.SfxVolume,
            brightnessSlider != null ? brightnessSlider.value : committedSettings.Brightness,
            resolution.x,
            resolution.y,
            fullscreenToggle != null ? fullscreenToggle.isOn : committedSettings.Fullscreen,
            qualityDropdown != null ? qualityDropdown.value : committedSettings.QualityLevel
        );
    }

    private void WriteSettingsToUI(GameSettingsData settings)
    {
        masterVolumeSlider?.SetValueWithoutNotify(settings.MasterVolume);
        musicVolumeSlider?.SetValueWithoutNotify(settings.MusicVolume);
        sfxVolumeSlider?.SetValueWithoutNotify(settings.SfxVolume);
        brightnessSlider?.SetValueWithoutNotify(settings.Brightness);
        RefreshValueLabel(masterVolumeSlider);
        RefreshValueLabel(musicVolumeSlider);
        RefreshValueLabel(sfxVolumeSlider);
        RefreshValueLabel(brightnessSlider);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(settings.Fullscreen);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.SetValueWithoutNotify(
                Mathf.Clamp(settings.QualityLevel, 0, Mathf.Max(0, qualityDropdown.options.Count - 1))
            );
            qualityDropdown.RefreshShownValue();
        }

        SetResolutionDropdown(settings.ResolutionWidth, settings.ResolutionHeight);
    }

    private void BuildResolutionOptions()
    {
        availableResolutions.Clear();
        availableResolutions.AddRange(
            Screen.resolutions
                .Select(resolution => new Vector2Int(resolution.width, resolution.height))
                .Distinct()
                .OrderBy(resolution => resolution.x)
                .ThenBy(resolution => resolution.y)
        );

        if (availableResolutions.Count == 0)
        {
            availableResolutions.Add(new Vector2Int(Screen.width, Screen.height));
        }

        if (resolutionDropdown == null)
        {
            return;
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(
            availableResolutions.Select(resolution => $"{resolution.x} x {resolution.y}").ToList()
        );
    }

    private void BuildQualityOptions()
    {
        if (qualityDropdown == null)
        {
            return;
        }

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(QualitySettings.names.ToList());
    }

    private void SetResolutionDropdown(int width, int height)
    {
        if (resolutionDropdown == null || availableResolutions.Count == 0)
        {
            return;
        }

        int index = availableResolutions.FindIndex(
            resolution => resolution.x == width && resolution.y == height
        );
        if (index < 0)
        {
            index = FindNearestResolutionIndex(new Vector2Int(width, height));
        }

        resolutionDropdown.SetValueWithoutNotify(index);
        resolutionDropdown.RefreshShownValue();
    }

    private int FindNearestResolutionIndex(Vector2Int requested)
    {
        int nearestIndex = 0;
        long nearestDistance = long.MaxValue;

        for (int i = 0; i < availableResolutions.Count; i++)
        {
            long widthDifference = availableResolutions[i].x - requested.x;
            long heightDifference = availableResolutions[i].y - requested.y;
            long distance = widthDifference * widthDifference + heightDifference * heightDifference;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private Vector2Int GetSelectedResolution()
    {
        if (resolutionDropdown == null || resolutionDropdown.value < 0 ||
            resolutionDropdown.value >= availableResolutions.Count)
        {
            return new Vector2Int(committedSettings.ResolutionWidth, committedSettings.ResolutionHeight);
        }

        return availableResolutions[resolutionDropdown.value];
    }

    private void ResolveMissingReferences()
    {
        resolutionDropdown ??= FindSceneComponent<TMP_Dropdown>("ResolutionDropdown");
        fullscreenToggle ??= FindSceneComponent<Toggle>("FullscreenToggle");
        qualityDropdown ??= FindSceneComponent<TMP_Dropdown>("QualityDropdown");
        defaultButton ??= FindSceneComponent<Button>("DefaultButton");
        applyButton ??= FindSceneComponent<Button>("ApplyButton");
    }

    private static void RefreshValueLabel(Slider slider)
    {
        slider?.GetComponentInParent<SettingsSliderRowView>()?.Refresh();
    }

    private static T FindSceneComponent<T>(string objectName) where T : Component
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            T component = root.GetComponentsInChildren<T>(true)
                .FirstOrDefault(candidate => candidate.gameObject.name == objectName);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private void SetMasterVolume(float value)
    {
        SetVolume(AudioManager.MasterVolumeParameter, value);
    }

    private void SetMusicVolume(float value)
    {
        SetVolume(AudioManager.MusicVolumeParameter, value);
    }

    private void SetSfxVolume(float value)
    {
        SetVolume(AudioManager.SfxVolumeParameter, value);
        SetVolume(AudioManager.UIVolumeParameter, value);
    }

    private void SetBrightness(float value)
    {
        BrightnessController.Instance?.SetNormalizedBrightness(value);
        RefreshValueLabel(brightnessSlider);
    }

    private void SetVolume(string parameterName, float value)
    {
        if (mixer != null)
        {
            mixer.SetFloat(parameterName, AudioManager.NormalizedToDecibels(value));
        }
    }
}
