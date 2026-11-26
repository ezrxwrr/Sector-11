using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FlashlightToggle : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kosongkan saja jika script dipasang langsung di VisionConeLight.")]
    [SerializeField] private Light flashlight;

    [Header("Settings")]
    [SerializeField] private bool startTurnedOn = true;
    [SerializeField] private bool showStatusHUD = true;

    private bool isTurnedOn;

    public bool IsTurnedOn => isTurnedOn;

    private void Awake()
    {
        if (flashlight == null)
        {
            flashlight = GetComponent<Light>();
            if (flashlight == null)
            {
                flashlight = GetComponentInChildren<Light>(true);
            }
        }

        isTurnedOn = startTurnedOn;
        ApplyLightState();
    }

    private void Update()
    {
        if (ReadTogglePressed())
        {
            isTurnedOn = !isTurnedOn;
            ApplyLightState();
        }
    }

    private void ApplyLightState()
    {
        if (flashlight != null)
        {
            flashlight.enabled = isTurnedOn;
        }
    }

    private bool ReadTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.T);
#endif
    }

    private void OnGUI()
    {
        if (!showStatusHUD) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        string stateText = isTurnedOn ? "[T] FLASHLIGHT: ON" : "[T] FLASHLIGHT: OFF";
        GUI.Box(new Rect(20, Screen.height - 52, 200, 32), stateText, style);
    }
}