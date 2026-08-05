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

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsBackButton;

    private bool isLoading;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowMainMenu();

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
        }
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
            settingsBackButton.onClick.AddListener(ShowMainMenu);
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
            settingsBackButton.onClick.RemoveListener(ShowMainMenu);
        }
    }

    private void Play()
    {
        if (isLoading)
        {
            return;
        }

        StartCoroutine(LoadGameplayScene());
    }

    private IEnumerator LoadGameplayScene()
    {
        isLoading = true;
        SetButtonsInteractable(false);

        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
        }

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
        if (isLoading)
        {
            return;
        }

        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    private void ShowMainMenu()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainMenuRoot != null)
        {
            mainMenuRoot.SetActive(true);
        }
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
        SetButtonsInteractable(true);

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
        }

        ShowMainMenu();
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
}
