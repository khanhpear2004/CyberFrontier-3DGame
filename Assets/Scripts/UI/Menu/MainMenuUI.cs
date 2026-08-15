using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuUI : MonoBehaviour
{
    private const string GameplaySceneName = "CyberScene";

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject loadingOverlay;

    [Header("Transitions")]
    [SerializeField] private MenuPanelTransition mainMenuTransition;
    [SerializeField] private MenuPanelTransition settingsTransition;
    [SerializeField] private MenuPanelTransition loadingTransition;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsBackButton;

    [Header("Settings")]
    [SerializeField] private AudioSettingsController settingsController;

    private bool isLoading;
    private bool isTransitioning;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (settingsController == null)
        {
            settingsController = FindAnyObjectByType<AudioSettingsController>();
        }

        PrepareInitialPanelState();
    }

    private void Start()
    {
        StartCoroutine(ShowInitialMenu());
    }

    private void OnEnable()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(Play);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ShowSettings);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(Quit);
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(CloseSettings);
        }
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(Play);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(ShowSettings);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(Quit);
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveListener(CloseSettings);
        }
    }

    private void Play()
    {
        if (isLoading || isTransitioning)
        {
            return;
        }

        StartCoroutine(LoadGameplayScene());
    }

    private IEnumerator LoadGameplayScene()
    {
        isLoading = true;
        isTransitioning = true;
        SetButtonsInteractable(false);

        yield return HidePanel(mainMenuRoot, mainMenuTransition);
        SetPanelImmediate(settingsPanel, settingsTransition, false);
        yield return ShowPanel(loadingOverlay, loadingTransition);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Single);

        if (loadOperation == null)
        {
            Debug.LogError($"Could not start loading scene '{GameplaySceneName}'.", this);
            RestoreMenuAfterLoadFailure();
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private void ShowSettings()
    {
        if (isLoading || isTransitioning)
        {
            return;
        }

        StartCoroutine(ShowSettingsRoutine());
    }

    private void CloseSettings()
    {
        if (isLoading || isTransitioning)
        {
            return;
        }

        StartCoroutine(CloseSettingsRoutine());
    }

    private IEnumerator ShowInitialMenu()
    {
        isTransitioning = true;
        yield return ShowPanel(mainMenuRoot, mainMenuTransition);
        SetButtonsInteractable(true);
        isTransitioning = false;
    }

    private IEnumerator ShowSettingsRoutine()
    {
        isTransitioning = true;
        SetButtonsInteractable(false);
        settingsController?.BeginEdit();

        yield return HidePanel(mainMenuRoot, mainMenuTransition);
        yield return ShowPanel(settingsPanel, settingsTransition);

        isTransitioning = false;
    }

    private IEnumerator CloseSettingsRoutine()
    {
        isTransitioning = true;
        settingsController?.CancelPendingChanges();

        yield return HidePanel(settingsPanel, settingsTransition);
        yield return ShowPanel(mainMenuRoot, mainMenuTransition);

        SetButtonsInteractable(true);
        isTransitioning = false;
    }

    private void Quit()
    {
        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("Quit only closes the application in a player build.", this);
#endif
    }

    private void RestoreMenuAfterLoadFailure()
    {
        isLoading = false;
        isTransitioning = false;
        SetButtonsInteractable(true);
        SetPanelImmediate(loadingOverlay, loadingTransition, false);
        SetPanelImmediate(mainMenuRoot, mainMenuTransition, true);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (playButton != null)
        {
            playButton.interactable = interactable;
        }

        if (settingsButton != null)
        {
            settingsButton.interactable = interactable;
        }

        if (quitButton != null)
        {
            quitButton.interactable = interactable;
        }
    }

    private void PrepareInitialPanelState()
    {
        SetPanelImmediate(mainMenuRoot, mainMenuTransition, false);
        SetPanelImmediate(settingsPanel, settingsTransition, false);
        SetPanelImmediate(loadingOverlay, loadingTransition, false);
        SetButtonsInteractable(false);
    }

    private static IEnumerator ShowPanel(GameObject panel, MenuPanelTransition transition)
    {
        if (panel == null)
        {
            yield break;
        }

        if (transition != null)
        {
            yield return transition.Show();
        }
        else
        {
            panel.SetActive(true);
        }
    }

    private static IEnumerator HidePanel(GameObject panel, MenuPanelTransition transition)
    {
        if (panel == null)
        {
            yield break;
        }

        if (transition != null)
        {
            yield return transition.Hide();
        }
        else
        {
            panel.SetActive(false);
        }
    }

    private static void SetPanelImmediate(
        GameObject panel,
        MenuPanelTransition transition,
        bool visible)
    {
        if (panel == null)
        {
            return;
        }

        if (transition != null)
        {
            transition.SetVisibleImmediate(visible);
        }
        else
        {
            panel.SetActive(visible);
        }
    }
}
