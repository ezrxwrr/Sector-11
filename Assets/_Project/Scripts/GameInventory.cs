using System.Collections.Generic;
using UnityEngine;

public enum MaterialType
{
    Charcoal,
    FilterCloth,
    WaterPipe,
    Lamp
}

public class GameInventory : MonoBehaviour
{
    public static GameInventory Instance { get; private set; }

    private Dictionary<MaterialType, int> stash = new Dictionary<MaterialType, int>();
    private Dictionary<MaterialType, int> raidBackpack = new Dictionary<MaterialType, int>();

    // Currency awal 1.000.000
    public int Currency { get; private set; } = 1000000;

    // Status Fasilitas Base (SDG 11)
    public bool IsFiltrationBuilt { get; private set; }
    public bool IsLampBuilt { get; private set; }

    // Status Equipment dari Flea Market (terbawa ke _Combat)
    public string EquippedScopeName { get; private set; } = "Iron Sight";
    public float EquippedScopeMultiplier { get; private set; } = 1.0f;

    public bool HasEquippedHelmet { get; private set; } = false;
    public float EquippedHelmetSaveChance { get; private set; } = 0.45f;

    public bool HasEquippedBodyArmor { get; private set; } = false;
    public float EquippedBodyArmorReduction { get; private set; } = 0.40f;

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

    // --- CURRENCY & FLEA MARKET ---
    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || Currency < amount) return false;
        Currency -= amount;
        return true;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        Currency += amount;
    }

    public void AddStashItem(MaterialType type, int amount = 1)
    {
        if (amount <= 0) return;
        stash[type] += amount;
    }

    public void EquipScope(string scopeName, float multiplier)
    {
        EquippedScopeName = scopeName;
        EquippedScopeMultiplier = Mathf.Max(1.0f, multiplier);
    }

    public void EquipHelmetGear(float saveChance)
    {
        HasEquippedHelmet = true;
        EquippedHelmetSaveChance = Mathf.Clamp01(saveChance);
    }

    public void EquipBodyArmorGear(float reduction)
    {
        HasEquippedBodyArmor = true;
        EquippedBodyArmorReduction = Mathf.Clamp(reduction, 0f, 0.9f);
    }

    // --- CHEAT CODE UNTUK TESTING DI BASE ---
    public void ActivateBaseMaterialCheat()
    {
        foreach (MaterialType type in System.Enum.GetValues(typeof(MaterialType)))
        {
            if (stash[type] < 1)
            {
                stash[type] = 1;
            }
            else
            {
                stash[type] += 1;
            }
        }
        Debug.Log("[CHEAT] Granted +1 of all Base Materials (Charcoal, FilterCloth, WaterPipe, Lamp)!");
    }

    // --- RAID BACKPACK & STASH ---
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

    // --- CRAFTING: WATER FILTRATION (3 PARTS) ---
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

    // --- CRAFTING: BASE LAMP (1 PART: LAMP) ---
    public bool CanCraftLamp()
    {
        if (IsLampBuilt) return false;
        return stash[MaterialType.Lamp] >= 1;
    }

    public bool TryCraftLamp()
    {
        if (!CanCraftLamp()) return false;

        stash[MaterialType.Lamp] -= 1;
        IsLampBuilt = true;
        return true;
    }
}