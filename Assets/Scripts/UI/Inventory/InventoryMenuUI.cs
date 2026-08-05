using UnityEngine;

public sealed class InventoryMenuUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Testing")]
    [SerializeField] private bool startOpenForTesting;

    private float previousTimeScale = 1f;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private void Awake()
    {
        IsOpen = false;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Start()
    {
        if (startOpenForTesting)
        {
            Open();
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
        {
            return;
        }

        if (IsOpen)
        {
            Close();
            return;
        }

        if (!RewardPopupUI.IsGameplayInputBlocked)
        {
            Open();
        }
    }

    private void LateUpdate()
    {
        if (!IsOpen)
        {
            return;
        }

        // Bảo đảm script gameplay khác không khóa lại cursor.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Open()
    {
        if (IsOpen || panelRoot == null)
        {
            return;
        }

        previousTimeScale = Time.timeScale;

        IsOpen = true;
        panelRoot.SetActive(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        Time.timeScale = previousTimeScale;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (!IsOpen)
        {
            return;
        }

        Time.timeScale = previousTimeScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        IsOpen = false;
    }
}