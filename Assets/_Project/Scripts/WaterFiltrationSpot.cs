using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public class WaterFiltrationSpot : MonoBehaviour
{
    [Header("Visual Assembly Parts (Assign Child Shapes)")]
    [SerializeField] private GameObject emptyOutlineVisual;
    [SerializeField] private GameObject builtFiltrationVisual;
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        RefreshVisualState();
    }

    private void Update()
    {
        RefreshVisualState();

        if (!playerInRange) return;

        GameInventory inv = GameInventory.EnsureExists();
        if (!inv.IsFiltrationBuilt && inv.CanCraftWaterFiltration() && ReadInteractPressed())
        {
            if (inv.TryCraftWaterFiltration())
            {
                RefreshVisualState();
            }
        }
    }

    private void RefreshVisualState()
    {
        bool built = GameInventory.EnsureExists().IsFiltrationBuilt;
        if (emptyOutlineVisual != null) emptyOutlineVisual.SetActive(!built);
        if (builtFiltrationVisual != null) builtFiltrationVisual.SetActive(built);
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

        float width = 520f;
        float height = 46f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - 100f;

        string msg;
        if (inv.IsFiltrationBuilt)
        {
            msg = "WATER FILTRATION SYSTEM: ONLINE (SDG 11 CLEAN WATER)";
        }
        else if (inv.CanCraftWaterFiltration())
        {
            msg = "PRESS [F] / [E] TO BUILD WATER FILTRATION SYSTEM";
        }
        else
        {
            msg = $"MISSING PARTS — NEED: CHARCOAL ({inv.GetStashCount(MaterialType.Charcoal)}/1), CLOTH ({inv.GetStashCount(MaterialType.FilterCloth)}/1), PIPE ({inv.GetStashCount(MaterialType.WaterPipe)}/1)";
        }

        GUI.Box(new Rect(x, y, width, height), msg, style);
    }
}