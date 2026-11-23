using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryHUD : MonoBehaviour
{
    [Tooltip("Centang TRUE saat dipasang di Player dalam scene _Base. Kosongkan (FALSE) di scene _Combat.")]
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

    private void OnGUI()
    {
        GameInventory inv = GameInventory.EnsureExists();

        GUIStyle hintStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Box(new Rect(Screen.width - 240, 20, 220, 32), "[TAB / I] INVENTORY", hintStyle);

        if (!isOpen) return;

        float width = 400f;
        float height = 270f;
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - height) * 0.5f;

        GUIStyle panelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };

        string title = isInBase ? "SAFE HOUSE STASH (SDG 11)" : "RAID BACKPACK (EXTRACT TO KEEP)";
        GUI.Box(new Rect(x, y, width, height), "\n" + title, panelStyle);

        GUIStyle rowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };

        int charcoal = isInBase ? inv.GetStashCount(MaterialType.Charcoal) : inv.GetBackpackCount(MaterialType.Charcoal);
        int cloth = isInBase ? inv.GetStashCount(MaterialType.FilterCloth) : inv.GetBackpackCount(MaterialType.FilterCloth);
        int pipe = isInBase ? inv.GetStashCount(MaterialType.WaterPipe) : inv.GetBackpackCount(MaterialType.WaterPipe);

        GUI.Label(new Rect(x + 30, y + 65, 340, 28), $"1. Activated Charcoal:   {charcoal} / 1", rowStyle);
        GUI.Label(new Rect(x + 30, y + 100, 340, 28), $"2. Micro-Filter Cloth:   {cloth} / 1", rowStyle);
        GUI.Label(new Rect(x + 30, y + 135, 340, 28), $"3. PVC Water Pipe:       {pipe} / 1", rowStyle);

        string status = inv.IsFiltrationBuilt
            ? "WATER FILTRATION: OPERATIONAL (CLEAN WATER ACTIVE)"
            : "GOAL: COLLECT ALL 3 PARTS FOR BASE WATER FILTER";

        GUIStyle statusStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Box(new Rect(x + 20, y + 185, width - 40, 45), status, statusStyle);
    }
}