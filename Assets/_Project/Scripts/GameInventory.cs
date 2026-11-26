using System;
using System.Collections.Generic;
using UnityEngine;

public enum MaterialType
{
    Charcoal,
    FilterCloth,
    WaterPipe,
    Wiring,
    Compressor,
    EngineParts,
    Fuel
}

public enum RaidResult { None, Extracted, Died }
public static class ItemInfo
{
    public static string GetName(MaterialType t)
    {
        switch (t)
        {
            case MaterialType.Charcoal:    return "Activated Charcoal";
            case MaterialType.FilterCloth: return "Micro-Filter Cloth";
            case MaterialType.WaterPipe:   return "PVC Water Pipe";
            case MaterialType.Wiring:      return "Wiring";
            case MaterialType.Compressor:  return "Compressor";
            case MaterialType.EngineParts: return "Engine Parts";
            case MaterialType.Fuel:        return "Fuel";
            default:                       return t.ToString();
        }
    }

    public static int GetSellPrice(MaterialType t)
    {
        switch (t)
        {
            case MaterialType.Charcoal:    return 20;
            case MaterialType.FilterCloth: return 25;
            case MaterialType.WaterPipe:   return 30;
            case MaterialType.Wiring:      return 40;
            case MaterialType.Compressor:  return 120;
            case MaterialType.EngineParts: return 150;
            case MaterialType.Fuel:        return 50;
            default:                       return 10;
        }
    }

    public static int GetBuyPrice(MaterialType t) => GetSellPrice(t) * 2;
}

public class GameInventory : MonoBehaviour
{
    public static GameInventory Instance { get; private set; }

    private Dictionary<MaterialType, int> stash = new Dictionary<MaterialType, int>();
    private Dictionary<MaterialType, int> raidBackpack = new Dictionary<MaterialType, int>();

    private Dictionary<MaterialType, int> lastRaidItems = new Dictionary<MaterialType, int>();

    public bool IsFiltrationBuilt { get; private set; }
    public int Money { get; private set; }
    public RaidResult LastRaidResult { get; private set; } = RaidResult.None;

    [Tooltip("Sırt çantasına sığan toplam item sayısı")]
    public int BackpackCapacity = 10;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (MaterialType type in Enum.GetValues(typeof(MaterialType)))
        {
            stash[type] = 0;
            raidBackpack[type] = 0;
            lastRaidItems[type] = 0;
        }
    }

    public static GameInventory EnsureExists()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("GameInventoryManager");
        return go.AddComponent<GameInventory>();
    }

 
    public void AddRaidLoot(MaterialType type, int amount = 1)
    {
        if (amount <= 0) return;
        raidBackpack[type] += amount;
        OnChanged?.Invoke();
    }

    public bool TryAddRaidLoot(MaterialType type, int amount = 1)
    {
        if (amount <= 0 || !CanCarry(amount)) return false;
        AddRaidLoot(type, amount);
        return true;
    }

    public int GetBackpackTotal()
    {
        int total = 0;
        foreach (var kv in raidBackpack) total += kv.Value;
        return total;
    }

    public bool CanCarry(int amount = 1) => GetBackpackTotal() + amount <= BackpackCapacity;

    public void SecureRaidLootToStash()
    {
        foreach (MaterialType type in Enum.GetValues(typeof(MaterialType)))
        {
            lastRaidItems[type] = raidBackpack[type];
            stash[type] += raidBackpack[type];
            raidBackpack[type] = 0;
        }
        LastRaidResult = RaidResult.Extracted;
        OnChanged?.Invoke();
    }

    public void LoseRaidLootOnDeath()
    {
        foreach (MaterialType type in Enum.GetValues(typeof(MaterialType)))
        {
            lastRaidItems[type] = raidBackpack[type];
            raidBackpack[type] = 0;
        }
        LastRaidResult = RaidResult.Died;
        OnChanged?.Invoke();
    }

    public int GetStashCount(MaterialType type) => stash[type];
    public int GetBackpackCount(MaterialType type) => raidBackpack[type];

    public int GetLastRaidCount(MaterialType type) => lastRaidItems[type];
        public void ClearRaidSummary()
    {
        foreach (MaterialType type in Enum.GetValues(typeof(MaterialType)))
            lastRaidItems[type] = 0;
        LastRaidResult = RaidResult.None;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        Money += amount;
        OnChanged?.Invoke();
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0 || Money < amount) return false;
        Money -= amount;
        OnChanged?.Invoke();
        return true;
    }

    public bool TrySellFromStash(MaterialType type, int amount = 1)
    {
        if (amount <= 0 || stash[type] < amount) return false;
        stash[type] -= amount;
        Money += ItemInfo.GetSellPrice(type) * amount;
        OnChanged?.Invoke();
        return true;
    }

    public bool TryBuyToStash(MaterialType type, int amount = 1)
    {
        if (amount <= 0) return false;
        int cost = ItemInfo.GetBuyPrice(type) * amount;
        if (Money < cost) return false;
        Money -= cost;
        stash[type] += amount;
        OnChanged?.Invoke();
        return true;
    }

    public void AddToStash(MaterialType type, int amount = 1)
    {
        if (amount <= 0) return;
        stash[type] += amount;
        OnChanged?.Invoke();
    }


    public bool CanCraftWaterFiltration()
    {
        if (IsFiltrationBuilt) return false;
        return stash[MaterialType.Charcoal] >= 1 &&
               stash[MaterialType.FilterCloth] >= 1 &&
               stash[MaterialType.WaterPipe] >= 1;
    }

    public bool TryCraftWaterFiltration()
    {
        if (!CanCraftWaterFiltration()) return false;

        stash[MaterialType.Charcoal] -= 1;
        stash[MaterialType.FilterCloth] -= 1;
        stash[MaterialType.WaterPipe] -= 1;
        IsFiltrationBuilt = true;
        OnChanged?.Invoke();
        return true;
    }
}