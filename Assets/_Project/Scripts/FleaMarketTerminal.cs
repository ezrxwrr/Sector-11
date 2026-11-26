using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FleaMarketTerminal : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;
    private bool isOpen;
    private string statusMessage = "Select an item to purchase with Credits (CR).";

    private void Update()
    {
        if (ReadMarketTogglePressed())
        {
            isOpen = !isOpen;
        }
        else if (playerInRange && ReadInteractPressed())
        {
            isOpen = !isOpen;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = false;
            isOpen = false;
        }
    }

    private bool ReadMarketTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.M);
#endif
    }

    private bool ReadInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;
        return Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E);
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

        // Tombol cepat di pojok kiri atas (di bawah tombol ENTER GAME)
        if (GUI.Button(new Rect(20, 68, 220, 38), "[M] FLEA MARKET", btnStyle))
        {
            isOpen = !isOpen;
        }

        if (playerInRange && !isOpen)
        {
            GUIStyle promptStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Box(new Rect((Screen.width - 360f) * 0.5f, Screen.height - 95f, 360f, 40f),
                "PRESS [F] / [M] TO OPEN FLEA MARKET", promptStyle);
        }

        if (!isOpen) return;

        float width = 560f;
        float height = 510f;
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - height) * 0.5f;

        GUIStyle panelStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };
        GUI.Box(new Rect(x, y, width, height), $"\nSECTOR 11 — FLEA MARKET   |   BALANCE: {inv.Currency:N0} CR", panelStyle);

        if (GUI.Button(new Rect(x + width - 45, y + 12, 32, 28), "X", btnStyle))
        {
            isOpen = false;
        }

        float rowY = y + 55f;
        float rowStep = 38f;

        // --- WEAPON OPTICS / SCOPES ---
        DrawSectionHeader(x + 20, rowY, "WEAPON OPTICS (AFFECTS HEADSHOT & ACCURACY)");
        rowY += 26f;

        DrawBuyRow(x + 20, rowY, "PK-06 Reflex Sight (1.4x Acc)", 45000, () =>
        {
            inv.EquipScope("PK-06 Reflex", 1.4f);
            statusMessage = "Purchased & Equipped: PK-06 Reflex Sight (1.4x)!";
        });
        rowY += rowStep;

        DrawBuyRow(x + 20, rowY, "EOTech Holographic + Magnifier (1.8x Acc)", 95000, () =>
        {
            inv.EquipScope("EOTech 1.8x", 1.8f);
            statusMessage = "Purchased & Equipped: EOTech 1.8x Magnifier!";
        });
        rowY += rowStep;

        DrawBuyRow(x + 20, rowY, "Trijicon ACOG 4x Scope (2.5x Acc)", 185000, () =>
        {
            inv.EquipScope("ACOG 4x Scope", 2.5f);
            statusMessage = "Purchased & Equipped: Trijicon ACOG 4x Scope (2.5x)!";
        });
        rowY += rowStep + 8f;

        // --- ARMOR & HELMET ---
        DrawSectionHeader(x + 20, rowY, "PROTECTIVE GEAR (AUTO-EQUIPPED IN RAID)");
        rowY += 26f;

        DrawBuyRow(x + 20, rowY, "6B47 Ratnik Helmet (45% Headshot Save)", 120000, () =>
        {
            inv.EquipHelmetGear(0.45f);
            statusMessage = "Purchased & Equipped: 6B47 Ratnik Helmet!";
        });
        rowY += rowStep;

        DrawBuyRow(x + 20, rowY, "6B13 Assault Body Armor (40% Torso Reduc)", 160000, () =>
        {
            inv.EquipBodyArmorGear(0.40f);
            statusMessage = "Purchased & Equipped: 6B13 Body Armor!";
        });
        rowY += rowStep + 8f;

        // --- HIDEOUT MATERIALS ---
        DrawSectionHeader(x + 20, rowY, "HIDEOUT MATERIALS (DIRECT TO STASH)");
        rowY += 26f;

        DrawBuyRow(x + 20, rowY, "Industrial Work Lamp (For Base Light)", 25000, () =>
        {
            inv.AddStashItem(MaterialType.Lamp, 1);
            statusMessage = $"Bought 1x Industrial Work Lamp (Stash: {inv.GetStashCount(MaterialType.Lamp)})";
        });
        rowY += rowStep;

        DrawBuyRow(x + 20, rowY, "Activated Charcoal (Filter Part 1/3)", 35000, () =>
        {
            inv.AddStashItem(MaterialType.Charcoal, 1);
            statusMessage = $"Bought 1x Activated Charcoal (Stash: {inv.GetStashCount(MaterialType.Charcoal)})";
        });
        rowY += rowStep;

        DrawBuyRow(x + 20, rowY, "Micro-Filter Cloth (Filter Part 2/3)", 40000, () =>
        {
            inv.AddStashItem(MaterialType.FilterCloth, 1);
            statusMessage = $"Bought 1x Micro-Filter Cloth (Stash: {inv.GetStashCount(MaterialType.FilterCloth)})";
        });
        rowY += rowStep;

        DrawBuyRow(x + 20, rowY, "PVC Water Pipe (Filter Part 3/3)", 50000, () =>
        {
            inv.AddStashItem(MaterialType.WaterPipe, 1);
            statusMessage = $"Bought 1x PVC Water Pipe (Stash: {inv.GetStashCount(MaterialType.WaterPipe)})";
        });

        // Status bar di bawah panel
        GUIStyle statusStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Box(new Rect(x + 20, y + height - 46, width - 40, 32), statusMessage, statusStyle);
    }

    private void DrawSectionHeader(float x, float y, string text)
    {
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(x, y, 520, 22), text, headerStyle);
    }

    private void DrawBuyRow(float x, float y, string itemLabel, int price, System.Action onPurchased)
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft
        };

        GUIStyle buyBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        GUI.Label(new Rect(x, y, 360, 30), itemLabel, labelStyle);

        if (GUI.Button(new Rect(x + 365, y, 155, 30), $"BUY ({price:N0} CR)", buyBtnStyle))
        {
            GameInventory inv = GameInventory.EnsureExists();
            if (inv.TrySpendMoney(price))
            {
                onPurchased?.Invoke();
            }
            else
            {
                statusMessage = "INSUFFICIENT CREDITS!";
            }
        }
    }
}