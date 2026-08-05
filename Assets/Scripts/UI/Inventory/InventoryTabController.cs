using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryTabController : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button loadoutTabButton;

    [Header("Pages")]
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private GameObject loadoutPage;

    private void Awake()
    {
        ValidateReferences();

        if (inventoryTabButton != null)
        {
            inventoryTabButton.onClick.AddListener(ShowInventoryPage);
        }

        if (loadoutTabButton != null)
        {
            loadoutTabButton.onClick.AddListener(ShowLoadoutPage);
        }
    }

    private void OnEnable()
    {
        if (HasRequiredReferences())
        {
            ShowInventoryPage();
        }
    }

    private void OnDestroy()
    {
        if (inventoryTabButton != null)
        {
            inventoryTabButton.onClick.RemoveListener(ShowInventoryPage);
        }

        if (loadoutTabButton != null)
        {
            loadoutTabButton.onClick.RemoveListener(ShowLoadoutPage);
        }
    }

    public void ShowInventoryPage()
    {
        SetPage(true);
    }

    public void ShowLoadoutPage()
    {
        SetPage(false);
    }

    private void SetPage(bool showInventory)
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        inventoryPage.SetActive(showInventory);
        loadoutPage.SetActive(!showInventory);

        inventoryTabButton.interactable = !showInventory;
        loadoutTabButton.interactable = showInventory;
    }

    private bool HasRequiredReferences()
    {
        return inventoryTabButton != null &&
               loadoutTabButton != null &&
               inventoryPage != null &&
               loadoutPage != null;
    }

    private void ValidateReferences()
    {
        if (inventoryTabButton == null)
        {
            Debug.LogError($"{name}: InventoryTabButton has not been assigned.", this);
        }

        if (loadoutTabButton == null)
        {
            Debug.LogError($"{name}: LoadoutTabButton has not been assigned.", this);
        }

        if (inventoryPage == null)
        {
            Debug.LogError($"{name}: InventoryPage has not been assigned.", this);
        }

        if (loadoutPage == null)
        {
            Debug.LogError($"{name}: LoadoutPage has not been assigned.", this);
        }
    }
}