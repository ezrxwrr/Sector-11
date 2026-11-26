using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RaidSummaryUI : MonoBehaviour
{
    private Font font;
    private float shownAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TrySpawn();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySpawn();
    }

    private static void TrySpawn()
    {
        GameInventory inv = GameInventory.Instance;
        if (inv == null || inv.LastRaidResult == RaidResult.None) return;
        if (FindFirstObjectByType<RaidSummaryUI>() != null) return;
        new GameObject("RaidSummaryUI").AddComponent<RaidSummaryUI>();
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shownAt = Time.unscaledTime;
        Build();
    }

    private void Update()
    {
        if (Time.unscaledTime - shownAt < 0.4f) return;
        if (ReadConfirmPressed()) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (GameInventory.Instance != null)
            GameInventory.Instance.ClearRaidSummary();
    }

    private bool ReadConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;
        return Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    private void Build()
    {
        GameInventory inv = GameInventory.Instance;
        bool died = inv.LastRaidResult == RaidResult.Died;

        var canvasGO = new GameObject("SummaryCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        Image backdrop = NewImage("Backdrop", canvasGO.transform, new Color(0f, 0f, 0f, 0.8f));
        RectTransform brt = backdrop.rectTransform;
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        Image panelImg = NewImage("Panel", canvasGO.transform, new Color(0.05f, 0.05f, 0.07f, 0.96f));
        RectTransform prt = panelImg.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(560, 100);

        var vlg = panelImg.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(28, 28, 24, 24);
        vlg.spacing = 10;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = panelImg.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Transform p = panelImg.transform;

        Text title = NewText("Title", p, died ? 64 : 44, TextAnchor.MiddleCenter, true);
        title.text = died ? "YOU DIED" : "EXTRACTION SUCCESS";
        title.color = died ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.45f, 0.95f, 0.5f);
        SetSize(title.gameObject, -1, died ? 90 : 70);

        Text sub = NewText("Sub", p, 26, TextAnchor.MiddleCenter, true);
        sub.text = died ? "ITEMS LOST" : "ITEMS SECURED IN STASH";
        sub.color = new Color(1f, 0.85f, 0.4f);
        SetSize(sub.gameObject, -1, 40);

        bool any = false;
        foreach (MaterialType t in System.Enum.GetValues(typeof(MaterialType)))
        {
            int count = inv.GetLastRaidCount(t);
            if (count <= 0) continue;
            any = true;
            BuildRow(p, ItemInfo.GetName(t), "x " + count);
        }

        if (!any)
        {
            Text none = NewText("None", p, 24, TextAnchor.MiddleCenter, false);
            none.text = died ? "YOU CARRIED NOTHING" : "YOU CARRIED NOTHING BACK";
            SetSize(none.gameObject, -1, 44);
        }

        Text footer = NewText("Footer", p, 22, TextAnchor.MiddleCenter, true);
        footer.text = "PRESS [ENTER] TO CONTINUE";
        footer.color = new Color(0.75f, 0.75f, 0.8f);
        SetSize(footer.gameObject, -1, 50);
    }

    private void BuildRow(Transform parent, string label, string value)
    {
        Image row = NewImage("Row", parent, new Color(0.14f, 0.14f, 0.18f, 1f));
        SetSize(row.gameObject, -1, 50);

        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 6, 6);
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        Text name = NewText("Name", row.transform, 26, TextAnchor.MiddleLeft, false);
        name.text = label;
        name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        Text count = NewText("Count", row.transform, 28, TextAnchor.MiddleRight, true);
        count.text = value;
        SetSize(count.gameObject, 110, -1);
    }

    private RectTransform NewRect(string n, Transform parent)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private Image NewImage(string n, Transform parent, Color c)
    {
        var img = NewRect(n, parent).gameObject.AddComponent<Image>();
        img.color = c;
        return img;
    }

    private Text NewText(string n, Transform parent, int size, TextAnchor anchor, bool bold)
    {
        var t = NewRect(n, parent).gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.alignment = anchor;
        t.color = Color.white;
        return t;
    }

    private void SetSize(GameObject go, float w, float h)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (w >= 0) le.preferredWidth = w;
        if (h >= 0) le.preferredHeight = h;
    }
}