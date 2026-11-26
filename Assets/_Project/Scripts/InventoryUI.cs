using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryUI : MonoBehaviour
{
    [Tooltip("_Base sahnesinde TRUE, _Combat sahnesinde FALSE")]
    [SerializeField] private bool isInBase = false;

    private GameObject panel;
    private Text titleText, infoText, statusText;
    private Text[] countTexts;
    private MaterialType[] types;
    private Font font;
    private bool isOpen;

    private static readonly Color Green = new Color(0.45f, 0.95f, 0.5f);
    private const int FiltrationRequired = 1;

    private void Awake()
    {
        GameInventory.EnsureExists();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        types = (MaterialType[])System.Enum.GetValues(typeof(MaterialType));
        BuildUI();
        panel.SetActive(false);
    }

    private void Update()
    {
        if (ReadTogglePressed())
        {
            isOpen = !isOpen;
            panel.SetActive(isOpen);
        }

#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM

        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            var inv = GameInventory.EnsureExists();
            foreach (var t in types) inv.AddToStash(t, 1);
            inv.AddMoney(100);
        }
#endif

        if (isOpen) Refresh();
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

    private bool IsFiltrationPart(MaterialType t) =>
        t == MaterialType.Charcoal || t == MaterialType.FilterCloth || t == MaterialType.WaterPipe;

    private void Refresh()
    {
        GameInventory inv = GameInventory.EnsureExists();

        titleText.text = isInBase ? "SAFE HOUSE STASH" : "RAID BACKPACK (EXTRACT TO KEEP)";
        infoText.text = isInBase
            ? $"MONEY: ${inv.Money}"
            : $"CAPACITY: {inv.GetBackpackTotal()} / {inv.BackpackCapacity}";

        for (int i = 0; i < types.Length; i++)
        {
            MaterialType t = types[i];
            int count = isInBase ? inv.GetStashCount(t) : inv.GetBackpackCount(t);

            if (IsFiltrationPart(t))
            {
                countTexts[i].text = $"{count} / {FiltrationRequired}";
                countTexts[i].color = count >= FiltrationRequired ? Green : Color.white;
            }
            else
            {
                countTexts[i].text = count.ToString();
                countTexts[i].color = count > 0 ? Green : Color.white;
            }
        }

        statusText.text = inv.IsFiltrationBuilt
            ? "WATER FILTRATION: OPERATIONAL"
            : "GOAL: COLLECT CHARCOAL, CLOTH AND PIPE FOR THE WATER FILTER";
    }


    private void BuildUI()
    {
        var canvasGO = new GameObject("InventoryCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        BuildHintButton(canvasGO.transform);

        Image panelImg = NewImage("Panel", canvasGO.transform, new Color(0.05f, 0.05f, 0.07f, 0.94f));
        panel = panelImg.gameObject;
        RectTransform prt = panelImg.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(600, 100);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(28, 28, 20, 20);
        vlg.spacing = 8;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = NewText("Title", panel.transform, 32, TextAnchor.MiddleCenter, true);
        SetSize(titleText.gameObject, -1, 48);

        infoText = NewText("Info", panel.transform, 26, TextAnchor.MiddleCenter, true);
        infoText.color = new Color(1f, 0.85f, 0.4f);
        SetSize(infoText.gameObject, -1, 38);

        countTexts = new Text[types.Length];
        for (int i = 0; i < types.Length; i++)
            BuildRow(i);

        Image statusBox = NewImage("StatusBox", panel.transform, new Color(0.15f, 0.15f, 0.2f, 1f));
        SetSize(statusBox.gameObject, -1, 64);
        statusText = NewText("StatusText", statusBox.transform, 20, TextAnchor.MiddleCenter, false);
        RectTransform srt = statusText.rectTransform;
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = new Vector2(10, 5);
        srt.offsetMax = new Vector2(-10, -5);
    }

    private void BuildHintButton(Transform canvasTransform)
    {
        Image box = NewImage("HintButton", canvasTransform, new Color(0.22f, 0.22f, 0.24f, 0.95f));
        var outline = box.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        RectTransform rt = box.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-24, -24);
        rt.sizeDelta = new Vector2(300, 56);

        Text label = NewText("Label", box.transform, 26, TextAnchor.MiddleCenter, true);
        label.text = "[TAB / I] INVENTORY";
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }

    private void BuildRow(int i)
    {
        MaterialType t = types[i];

        Image row = NewImage("Row_" + t, panel.transform, new Color(0.14f, 0.14f, 0.18f, 1f));
        SetSize(row.gameObject, -1, 56);

        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 6, 6);
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        Color c = Color.HSVToRGB((float)i / types.Length, 0.45f, 0.75f);
        Image icon = NewImage("Icon", row.transform, c);
        SetSize(icon.gameObject, 40, 40);

        Text name = NewText("Name", row.transform, 26, TextAnchor.MiddleLeft, false);
        name.text = ItemInfo.GetName(t);
        name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        Text count = NewText("Count", row.transform, 28, TextAnchor.MiddleRight, true);
        SetSize(count.gameObject, 110, -1);
        countTexts[i] = count;
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