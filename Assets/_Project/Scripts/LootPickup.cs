using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public class LootPickup : MonoBehaviour
{
    [SerializeField] private MaterialType materialType = MaterialType.Charcoal;
    [SerializeField] private string displayName = "Activated Charcoal";
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);

        if (!playerInRange) return;

        if (ReadInteractPressed())
        {
            GameInventory.EnsureExists().AddRaidLoot(materialType, 1);
            Health.SetHitFeedback($"LOOTED: {displayName.ToUpper()}");
            Destroy(gameObject);
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
        }
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

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        float width = 360f;
        float height = 40f;
        GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height - 95f, width, height),
            $"PRESS [F] / [E] TO LOOT {displayName.ToUpper()}", style);
    }
}