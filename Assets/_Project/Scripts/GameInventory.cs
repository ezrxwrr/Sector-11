using System.Collections.Generic;
using UnityEngine;

public enum MaterialType
{
    Charcoal,
    FilterCloth,
    WaterPipe
}

public class GameInventory : MonoBehaviour
{
    public static GameInventory Instance { get; private set; }

    private Dictionary<MaterialType, int> stash = new Dictionary<MaterialType, int>();
    private Dictionary<MaterialType, int> raidBackpack = new Dictionary<MaterialType, int>();

    public bool IsFiltrationBuilt { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (MaterialType type in System.Enum.GetValues(typeof(MaterialType)))
        {
            stash[type] = 0;
            raidBackpack[type] = 0;
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
    }

    public void SecureRaidLootToStash()
    {
        foreach (MaterialType type in System.Enum.GetValues(typeof(MaterialType)))
        {
            stash[type] += raidBackpack[type];
            raidBackpack[type] = 0;
        }
    }

    public void LoseRaidLootOnDeath()
    {
        foreach (MaterialType type in System.Enum.GetValues(typeof(MaterialType)))
        {
            raidBackpack[type] = 0;
        }
    }

    public int GetStashCount(MaterialType type) => stash[type];
    public int GetBackpackCount(MaterialType type) => raidBackpack[type];

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
        return true;
    }
}