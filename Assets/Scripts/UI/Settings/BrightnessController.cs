using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BrightnessController : MonoBehaviour
{
    public static BrightnessController Instance { get; private set; }

    [Header("Profile")]
    [SerializeField] private VolumeProfile baseProfile;

    [Header("Overlay UI Background")]
    [SerializeField] private Material uiBackgroundMaterial;

    [Header("Exposure Range")]
    [SerializeField] private float minimumExposure = -1.5f;
    [SerializeField] private float maximumExposure = 1.5f;

    private Volume runtimeVolume;
    private VolumeProfile runtimeProfile;
    private ColorAdjustments colorAdjustments;
    private Material runtimeUiBackgroundMaterial;

    private static readonly int ExposureProperty = Shader.PropertyToID("_Exposure");

    public float CurrentBrightness { get; private set; } = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (baseProfile == null)
        {
            Debug.LogError(
                "BrightnessController chưa được gắn BrightnessVolumeProfile.",
                this);
        }
        else
        {
            runtimeProfile = Instantiate(baseProfile);
            runtimeProfile.name = $"{baseProfile.name} (Runtime)";

            runtimeVolume = GetComponent<Volume>();

            if (runtimeVolume == null)
            {
                runtimeVolume = gameObject.AddComponent<Volume>();
            }

            runtimeVolume.isGlobal = true;
            runtimeVolume.priority = 100f;
            runtimeVolume.weight = 1f;
            runtimeVolume.sharedProfile = runtimeProfile;

            if (!runtimeProfile.TryGet(out colorAdjustments))
            {
                colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);
            }
        }

        if (uiBackgroundMaterial != null)
        {
            runtimeUiBackgroundMaterial = Instantiate(uiBackgroundMaterial);
            runtimeUiBackgroundMaterial.name = $"{uiBackgroundMaterial.name} (Runtime)";
        }

        SetNormalizedBrightness(GameSettingsStorage.Load().Brightness);
        BindMainMenuBackground(SceneManager.GetActiveScene());
    }

    public void SetNormalizedBrightness(float normalizedBrightness)
    {
        CurrentBrightness = Mathf.Clamp01(normalizedBrightness);

        float exposure = Mathf.Lerp(
            minimumExposure,
            maximumExposure,
            CurrentBrightness);

        colorAdjustments?.postExposure.Override(exposure);

        if (runtimeUiBackgroundMaterial != null)
        {
            runtimeUiBackgroundMaterial.SetFloat(ExposureProperty, exposure);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindMainMenuBackground(scene);
        SetNormalizedBrightness(CurrentBrightness);
    }

    private void BindMainMenuBackground(Scene scene)
    {
        if (runtimeUiBackgroundMaterial == null || !scene.IsValid())
        {
            return;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != "MainMenuCanvas")
            {
                continue;
            }

            Transform background = root.transform.Find("Background");
            Image backgroundImage = background != null ? background.GetComponent<Image>() : null;
            if (backgroundImage != null)
            {
                backgroundImage.material = runtimeUiBackgroundMaterial;
            }

            return;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }

        if (runtimeProfile != null)
        {
            Destroy(runtimeProfile);
        }

        if (runtimeUiBackgroundMaterial != null)
        {
            Destroy(runtimeUiBackgroundMaterial);
        }
    }
}
