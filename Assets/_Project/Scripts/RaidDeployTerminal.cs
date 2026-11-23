using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public class RaidDeployTerminal : MonoBehaviour
{
    [SerializeField] private string combatSceneName = "_Combat";
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange;
    private bool showDeployMenu;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (playerInRange && ReadInteractPressed())
        {
            showDeployMenu = !showDeployMenu;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = true;
            showDeployMenu = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = false;
            showDeployMenu = false;
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
        GUIStyle quickBtn = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };

        if (GUI.Button(new Rect(20, 20, 220, 40), "ENTER GAME (DEPLOY)", quickBtn))
        {
            SceneManager.LoadScene(combatSceneName);
        }

        if (!showDeployMenu) return;

        float width = 420f;
        float height = 225f;
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - height) * 0.5f;

        GUIStyle panel = new GUIStyle(GUI.skin.box)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };
        GUI.Box(new Rect(x, y, width, height), "\nSECTOR 11 — RAID DEPLOYMENT", panel);

        GUIStyle desc = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        GUI.Label(new Rect(x + 25, y + 55, width - 50, 75),
            "Objective: Scavenge Charcoal, Filter Cloth, and Water Pipe, then reach the Green Extraction Zone.", desc);

        if (GUI.Button(new Rect(x + 85, y + 145, 250, 48), "ENTER GAME (DEPLOY)", quickBtn))
        {
            SceneManager.LoadScene(combatSceneName);
        }
    }
}