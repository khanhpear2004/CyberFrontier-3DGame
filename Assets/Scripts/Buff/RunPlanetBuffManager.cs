using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunPlanetBuffManager : MonoBehaviour
{
    private readonly Dictionary<string, PlanetBuffData> ownedBuffs =
        new Dictionary<string, PlanetBuffData>();

    public event Action<PlanetBuffData, bool> BuffOwnershipChanged;

    public int OwnedBuffCount => ownedBuffs.Count;

    public bool AddBuff(PlanetBuffData buffData)
    {
        if (!CanUseBuff(buffData))
        {
            return false;
        }

        string buffId = buffData.BuffId;

        if (ownedBuffs.TryGetValue(buffId, out PlanetBuffData existingBuff))
        {
            if (existingBuff != buffData)
            {
                Debug.LogWarning($"{name}: Có hai PlanetBuffData khác nhau cùng BuffId {buffId}.", this);
            }

            return false;
        }

        ownedBuffs.Add(buffId, buffData);
        BuffOwnershipChanged?.Invoke(buffData, true);

        return true;
    }

    public bool RemoveBuff(string buffId)
    {
        if (string.IsNullOrWhiteSpace(buffId))
        {
            return false;
        }

        if (!ownedBuffs.TryGetValue(buffId, out PlanetBuffData buffData))
        {
            return false;
        }

        ownedBuffs.Remove(buffId);
        BuffOwnershipChanged?.Invoke(buffData, false);

        return true;
    }

    public bool HasBuff(string buffId)
    {
        if (string.IsNullOrWhiteSpace(buffId))
        {
            return false;
        }

        return ownedBuffs.ContainsKey(buffId);
    }

    public bool TryGetOwnedBuff(string buffId, out PlanetBuffData buffData)
    {
        if (string.IsNullOrWhiteSpace(buffId))
        {
            buffData = null;
            return false;
        }

        return ownedBuffs.TryGetValue(buffId, out buffData);
    }

    public IEnumerable<PlanetBuffData> GetOwnedBuffs()
    {
        foreach (PlanetBuffData buffData in ownedBuffs.Values)
        {
            yield return buffData;
        }
    }

    public void ClearAllBuffs()
    {
        if (ownedBuffs.Count == 0)
        {
            return;
        }

        List<PlanetBuffData> removedBuffs = new List<PlanetBuffData>(ownedBuffs.Values);

        ownedBuffs.Clear();

        foreach (PlanetBuffData buffData in removedBuffs)
        {
            BuffOwnershipChanged?.Invoke(buffData, false);
        }
    }

    private bool CanUseBuff(PlanetBuffData buffData)
    {
        if (buffData == null)
        {
            Debug.LogWarning($"{name}: Không thể thêm PlanetBuffData null.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(buffData.BuffId))
        {
            Debug.LogWarning(
                $"{name}: Planet Buff {buffData.name} chưa có BuffId.",
                buffData
            );

            return false;
        }

        return true;
    }
}