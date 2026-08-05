using System.Collections.Generic;
using UnityEngine;

public sealed class OwnedPlanetBuffListUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private RunPlanetBuffManager runPlanetBuffManager;
    [SerializeField] private PlanetBuffLoadoutManager buffLoadoutManager;

    [Header("List")]
    [SerializeField] private Transform content;
    [SerializeField] private PlanetBuffLoadoutSlotView buffViewPrefab;

    [Header("Detail")]
    [SerializeField] private PlanetBuffDetailController buffDetailController;

    private readonly List<PlanetBuffLoadoutSlotView> spawnedViews = new List<PlanetBuffLoadoutSlotView>();

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        if (runPlanetBuffManager != null)
        {
            runPlanetBuffManager.BuffOwnershipChanged += HandleBuffOwnershipChanged;
        }

        if (buffLoadoutManager != null)
        {
            buffLoadoutManager.LoadoutChanged += HandleLoadoutChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (runPlanetBuffManager != null)
        {
            runPlanetBuffManager.BuffOwnershipChanged -= HandleBuffOwnershipChanged;
        }

        if (buffLoadoutManager != null)
        {
            buffLoadoutManager.LoadoutChanged -= HandleLoadoutChanged;
        }
    }

    public void Refresh()
    {
        ClearSpawnedViews();

        if (!HasRequiredReferences())
        {
            return;
        }

        List<PlanetBuffData> availableBuffs = new List<PlanetBuffData>();

        foreach (PlanetBuffData buffData in runPlanetBuffManager.GetOwnedBuffs())
        {
            if (buffData == null || buffLoadoutManager.IsEquipped(buffData.BuffId))
            {
                continue;
            }

            availableBuffs.Add(buffData);
        }

        availableBuffs.Sort(CompareBuffsByName);

        foreach (PlanetBuffData buffData in availableBuffs)
        {
            PlanetBuffLoadoutSlotView buffView = Instantiate(buffViewPrefab, content);
            buffView.Bind(buffData, HandleBuffClicked);
            spawnedViews.Add(buffView);
        }
    }

    private void HandleBuffClicked(PlanetBuffData buffData)
    {
        if (buffData == null || buffDetailController == null)
        {
            return;
        }

        buffDetailController.ShowBuff(buffData);
    }

    private void HandleBuffOwnershipChanged(PlanetBuffData buffData, bool isOwned)
    {
        Refresh();
    }

    private void HandleLoadoutChanged()
    {
        Refresh();
    }

    private void ClearSpawnedViews()
    {
        foreach (PlanetBuffLoadoutSlotView spawnedView in spawnedViews)
        {
            if (spawnedView != null)
            {
                Destroy(spawnedView.gameObject);
            }
        }

        spawnedViews.Clear();
    }

    private bool HasRequiredReferences()
    {
        return runPlanetBuffManager != null &&
               buffLoadoutManager != null &&
               content != null &&
               buffViewPrefab != null;
    }

    private void ValidateReferences()
    {
        if (runPlanetBuffManager == null)
        {
            Debug.LogError($"{name}: RunPlanetBuffManager has not been assigned.", this);
        }

        if (buffLoadoutManager == null)
        {
            Debug.LogError($"{name}: PlanetBuffLoadoutManager has not been assigned.", this);
        }

        if (content == null)
        {
            Debug.LogError($"{name}: Content has not been assigned.", this);
        }

        if (buffViewPrefab == null)
        {
            Debug.LogError($"{name}: BuffViewPrefab has not been assigned.", this);
        }

        if (buffDetailController == null)
        {
            Debug.LogError($"{name}: BuffDetailController has not been assigned.", this);
        }
    }

    private static int CompareBuffsByName(PlanetBuffData left, PlanetBuffData right)
    {
        return string.Compare(left.DisplayName, right.DisplayName, System.StringComparison.Ordinal);
    }
}
