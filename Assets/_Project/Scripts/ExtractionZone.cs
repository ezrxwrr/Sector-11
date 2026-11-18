using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class ExtractionZone : MonoBehaviour
{
    [Header("Extraction Settings")]
    [SerializeField] private float extractionDuration = 5f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool reloadSceneOnExtract = true;

    [Header("Prototype HUD")]
    [SerializeField] private bool showExtractionHUD = true;

    [Header("Events")]
    public UnityEvent onExtractionComplete;

    private bool playerInside;
    private bool extracted;
    private float timer;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (extracted || !playerInside) return;

        timer += Time.deltaTime;

        if (timer >= extractionDuration)
        {
            extracted = true;
            Debug.Log("[ExtractionZone] EXTRACTION SUCCESS!");
            onExtractionComplete?.Invoke();

            if (reloadSceneOnExtract)
            {
                Invoke(nameof(ReloadScene), 2f);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInside = false;
            if (!extracted)
            {
                timer = 0f;
            }
        }
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnGUI()
    {
        if (!showExtractionHUD || (!playerInside && !extracted)) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        float width = 340f;
        float height = 48f;
        float x = (Screen.width - width) * 0.5f;
        float y = 40f;

        if (extracted)
        {
            GUI.Box(new Rect(x, y, width, height), "EXTRACTION SUCCESS!", style);
        }
        else
        {
            float remaining = Mathf.Max(0f, extractionDuration - timer);
            GUI.Box(new Rect(x, y, width, height), $"EXTRACTING IN: {remaining:F1}s", style);
        }
    }
}