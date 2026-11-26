using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryHUD : MonoBehaviour
{
    [SerializeField] private bool isInBase = false;
    private bool isOpen = false;

    private void Awake()
    {
        GameInventory.EnsureExists();
    }

    private void Update()
    {
        if (ReadTogglePressed())
        {
            isOpen = !isOpen;
        }

        // Cheat Code F1 khusus di Base untuk langsung spawn semua bahan crafting
        if (isInBase && ReadCheatPressed())
        {
            GameInventory.EnsureExists().ActivateBaseMaterialCheat();
            isOpen = true;
        }
    }

    private bool ReadTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;
        return Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.iKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I);
#endif
    }

    private bool ReadCheatPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F1);
#endif
    }

    private void OnGUI()
    {
        GameInventory inv = GameInventory.EnsureExists();

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };

        GUIStyle currencyStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        // Tampilkan saldo Currency di pojok kanan atas
        GUI.Box(new Rect(Screen.width - 240, 20, 220, 32), $"CR: {inv.Currency:N0}", currencyStyle);

        // Tombol buka/tutup Inventory
        if (GUI.Button(new Rect(Screen.width - 240, 58, 220, 34), "[TAB / I] INVENTORY", btnStyle))
        {
            isOpen = !isOpen;
        }

        // Tombol Cheat khusus di _Base
        if (isInBase)
        {
            if (GUI.Button(new Rect(Screen.width - 240, 98, 220, 34), "[F1] CHEAT: +ALL PARTS", btnStyle))
            {
                inv.ActivateBaseMaterialCheat();
                isOpen = true;
            }
        }

        if (!isOpen) return;

        float width = 430f;
        float height = 385f;
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - height) * 0.5f;

        GUIStyle panelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };

        string title = isInBase ? "SAFE HOUSE STASH & LOADOUT" : "RAID BACKPACK & LOADOUT";
        GUI.Box(new Rect(x, y, width, height), "\n" + title, panelStyle);

        GUIStyle sectionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };

        // Bagian Loadout Aktif
        string helmetText = inv.HasEquippedHelmet ? $"Equipped ({Mathf.RoundToInt(inv.EquippedHelmetSaveChance * 100)}% Save)" : "None";
        string armorText = inv.HasEquippedBodyArmor ? $"Equipped (-{Mathf.RoundToInt(inv.EquippedBodyArmorReduction * 100)}% Dmg)" : "None";

        GUI.Label(new Rect(x + 25, y + 55, 380, 24), $"Optic: {inv.EquippedScopeName} ({inv.EquippedScopeMultiplier:F2}x)", sectionStyle);
        GUI.Label(new Rect(x + 25, y + 80, 380, 24), $"Helmet: {helmetText}", sectionStyle);
        GUI.Label(new Rect(x + 25, y + 105, 380, 24), $"Body Armor: {armorText}", sectionStyle);

        // Bagian Material Hideout (SDG 11)
        int charcoal = isInBase ? inv.GetStashCount(MaterialType.Charcoal) : inv.GetBackpackCount(MaterialType.Charcoal);
        int cloth = isInBase ? inv.GetStashCount(MaterialType.FilterCloth) : inv.GetBackpackCount(MaterialType.FilterCloth);
        int pipe = isInBase ? inv.GetStashCount(MaterialType.WaterPipe) : inv.GetBackpackCount(MaterialType.WaterPipe);
        int lamp = isInBase ? inv.GetStashCount(MaterialType.Lamp) : inv.GetBackpackCount(MaterialType.Lamp);

        GUI.Label(new Rect(x + 25, y + 145, 380, 24), "--- HIDEOUT MATERIALS (SDG 11) ---", sectionStyle);
        GUI.Label(new Rect(x + 25, y + 172, 380, 24), $"1. Activated Charcoal:   {charcoal} / 1", sectionStyle);
        GUI.Label(new Rect(x + 25, y + 197, 380, 24), $"2. Micro-Filter Cloth:   {cloth} / 1", sectionStyle);
        GUI.Label(new Rect(x + 25, y + 222, 380, 24), $"3. PVC Water Pipe:       {pipe} / 1", sectionStyle);
        GUI.Label(new Rect(x + 25, y + 247, 380, 24), $"4. Industrial Work Lamp: {lamp} / 1", sectionStyle);

        string filterStatus = inv.IsFiltrationBuilt ? "WATER FILTER: ONLINE" : "WATER FILTER: NOT BUILT";
        string lampStatus = inv.IsLampBuilt ? "BASE LAMP: ONLINE" : "BASE LAMP: NOT BUILT";

        GUIStyle statusStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Box(new Rect(x + 20, y + 290, width - 40, 55), $"{filterStatus}  |  {lampStatus}\n(Press TAB / I to Close)", statusStyle);
    }
}