using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public class AmmoCrate : MonoBehaviour
{
    [Header("Ammo Resupply Settings")]
    [SerializeField] private int ammoPerPickup = 60;
    [Tooltip("Berapa kali peti ini bisa diambil. Isi 1 untuk sekali pakai, atau -1 untuk tak terbatas.")]
    [SerializeField] private int maxUses = 2;
    [SerializeField] private string playerTag = "Player";

    [Header("Interaction")]
    [SerializeField] private bool showPromptHUD = true;

    private int remainingUses;
    private bool playerInRange;
    private PlayerShooter targetShooter;

    public bool IsEmpty => maxUses > 0 && remainingUses <= 0;

    private void Awake()
    {
        remainingUses = maxUses;

        // Pastikan ada trigger collider untuk mendeteksi jarak interaksi Player
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            BoxCollider triggerZone = gameObject.AddComponent<BoxCollider>();
            triggerZone.isTrigger = true;
            triggerZone.size = new Vector3(2.4f, 2.0f, 2.4f);
        }
    }

    private void Update()
    {
        if (!playerInRange || IsEmpty || targetShooter == null) return;

        if (ReadInteractPressed())
        {
            TakeAmmo();
        }
    }

    private void TakeAmmo()
    {
        targetShooter.AddAmmo(ammoPerPickup);

        if (maxUses > 0)
        {
            remainingUses--;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = true;
            targetShooter = other.GetComponentInParent<PlayerShooter>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = false;
            targetShooter = null;
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
        if (!showPromptHUD || !playerInRange) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        float width = 320f;
        float height = 40f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - 95f;

        string prompt = IsEmpty
            ? "AMMO CRATE [EMPTY]"
            : (maxUses > 0
                ? $"PRESS [F] / [E] TO TAKE AMMO (+{ammoPerPickup}) [{remainingUses}x]"
                : $"PRESS [F] / [E] TO TAKE AMMO (+{ammoPerPickup})");

        GUI.Box(new Rect(x, y, width, height), prompt, style);
    }
}