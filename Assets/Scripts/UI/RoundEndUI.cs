using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RoundEndUI : MonoBehaviour
{
    private const string MainMenuSceneName = "Main Menu";

    [Header("Gameplay References")]
    [SerializeField] private ExtractionZone extractionZone;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private InventoryMenuUI inventoryMenuUI;
    [SerializeField] private PlayerDeathSequence playerDeathSequence;

    [Header("UI References")]
    [SerializeField] private GameObject screenRoot;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private bool isLoading;

    public bool HasEnded { get; private set; }
    public bool WasVictory { get; private set; }

    private void Awake()
    {
        HideAll();
    }

    private void OnEnable()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        extractionZone.ExtractionCompleted += HandleExtractionCompleted;
        playerDeathSequence.SequenceCompleted += HandlePlayerDeathSequenceCompleted;
        restartButton.onClick.AddListener(RestartRound);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (extractionZone.IsCompleted)
        {
            ShowEndScreen(true);
        }
       else if (playerDeathSequence.HasCompleted)
        {
            ShowEndScreen(false);
        }
    }

    private void OnDisable()
    {
        if (extractionZone != null)
        {
            extractionZone.ExtractionCompleted -= HandleExtractionCompleted;
        }

        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDeathSequenceCompleted;
        }

        if (playerDeathSequence != null)
        {
            playerDeathSequence.SequenceCompleted -= HandlePlayerDeathSequenceCompleted;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartRound);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }
    }

    private void LateUpdate()
    {
        if (!HasEnded)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (!HasEnded)
        {
            return;
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleExtractionCompleted()
    {
        ShowEndScreen(true);
    }

    private void HandlePlayerDeathSequenceCompleted()
    {
        ShowEndScreen(false);
    }

    private void RestartRound()
    {
        if (isLoading)
        {
            return;
        }

        PrepareSceneChange();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    private void ReturnToMainMenu()
    {
        if (isLoading)
        {
            return;
        }

        PrepareSceneChange();
        SceneManager.LoadScene(MainMenuSceneName, LoadSceneMode.Single);
    }

    private void PrepareSceneChange()
    {
        isLoading = true;
        restartButton.interactable = false;
        mainMenuButton.interactable = false;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowEndScreen(bool victory)
    {
        if (HasEnded)
        {
            return;
        }

        HasEnded = true;
        WasVictory = victory;

        if (inventoryMenuUI != null)
        {
            if (InventoryMenuUI.IsOpen)
            {
                inventoryMenuUI.Close();
            }

            inventoryMenuUI.enabled = false;
        }

        screenRoot.SetActive(true);
        victoryPanel.SetActive(victory);
        defeatPanel.SetActive(!victory);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log(victory ? "Round ended: Victory." : "Round ended: Defeat.", this);
    }

    private void HideAll()
    {
        HasEnded = false;
        WasVictory = false;

        if (screenRoot != null)
        {
            screenRoot.SetActive(false);
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }
    }

    private bool HasRequiredReferences()
    {
        if (extractionZone == null)
        {
            Debug.LogError($"{name}: Missing ExtractionZone.", this);
            return false;
        }

        if (playerHealth == null)
        {
            Debug.LogError($"{name}: Missing PlayerHealth.", this);
            return false;
        }

        if (screenRoot == null || victoryPanel == null || defeatPanel == null)
        {
            Debug.LogError($"{name}: Missing round-end UI references.", this);
            return false;
        }

        if (restartButton == null || mainMenuButton == null)
        {
            Debug.LogError($"{name}: Missing round-end button references.", this);
            return false;
        }

        if (playerDeathSequence == null)
        {
            Debug.LogError($"{name}: Missing PlayerDeathSequence.", this);
            return false;
        }

        return true;
    }
}
