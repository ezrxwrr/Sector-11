using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public class BaseLampSpot : MonoBehaviour
{
    [Header("Visual Assembly Parts (Assign Child Shapes)")]
    [SerializeField] private GameObject emptyOutlineVisual;
    [SerializeField] private GameObject builtLampVisual;
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;
    private bool isLampOn = true;
    private Light[] lampLights;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        if (builtLampVisual != null)
        {
            lampLights = builtLampVisual.GetComponentsInChildren<Light>(true);
        }

        RefreshVisualState();
    }

    private void Update()
    {
        RefreshVisualState();

        if (!playerInRange || !ReadInteractPressed()) return;

        GameInventory inv = GameInventory.EnsureExists();

        // 1. Jika belum dibangun -> bangun lampu memakai 1 material Lamp
        if (!inv.IsLampBuilt)
        {
            if (inv.CanCraftLamp() && inv.TryCraftLamp())
            {
                isLampOn = true;
                RefreshVisualState();
            }
        }
        // 2. Jika sudah dibangun -> toggle nyala/mati lampu
        else
        {
            isLampOn = !isLampOn;
            RefreshVisualState();
        }
    }

    private void RefreshVisualState()
    {
        bool built = GameInventory.EnsureExists().IsLampBuilt;

        if (emptyOutlineVisual != null)
        {
            emptyOutlineVisual.SetActive(!built);
        }

        if (builtLampVisual != null)
        {
            builtLampVisual.SetActive(built);
        }

        if (lampLights != null)
        {
            for (int i = 0; i < lampLights.Length; i++)
            {
                if (lampLights[i] != null)
                {
                    lampLights[i].enabled = built && isLampOn;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
            playerInRange = false;
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
        if (!playerInRange) return;

        GameInventory inv = GameInventory.EnsureExists();
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        float width = 480f;
        float height = 46f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - 100f;

        string msg;
        if (inv.IsLampBuilt)
        {
            msg = isLampOn
                ? "SHELTER LAMP: ON — PRESS [F] / [E] TO TURN OFF"
                : "SHELTER LAMP: OFF — PRESS [F] / [E] TO TURN ON";
        }
        else if (inv.CanCraftLamp())
        {
            msg = "PRESS [F] / [E] TO INSTALL BASE LAMP";
        }
        else
        {
            msg = $"MISSING PART — NEED: INDUSTRIAL WORK LAMP ({inv.GetStashCount(MaterialType.Lamp)}/1)";
        }

        GUI.Box(new Rect(x, y, width, height), msg, style);
    }
}