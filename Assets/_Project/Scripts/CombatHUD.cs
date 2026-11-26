using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CombatHUD : MonoBehaviour
{
    private class Toast
    {
        public Image bg;
        public Text text;
        public float expire;
    }

    private const float ToastDuration = 2.5f;
    private const float FadeTime = 0.6f;

    private static readonly Color Green = new Color(0.45f, 0.95f, 0.5f);
    private static readonly Color Red = new Color(0.85f, 0.2f, 0.2f);

    private Font font;
    private Health health;
    private PlayerShooter shooter;
    private MaterialType[] types;
    private int[] lastBackpack;
    private List<Toast> toasts = new List<Toast>();

    private RectTransform hpFill;
    private Image hpFillImage;
    private Text hpText, ammoText, carryText;
    private RectTransform toastRoot;
    private float nextFindTime;

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
        if (FindFirstObjectByType<PlayerShooter>() == null) return;
        if (FindFirstObjectByType<CombatHUD>() != null) return;
        new GameObject("CombatHUD").AddComponent<CombatHUD>();
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        types = (MaterialType[])System.Enum.GetValues(typeof(MaterialType));
        lastBackpack = new int[types.Length];
        BuildUI();
    }

    private void Start()
    {
        GameInventory inv = GameInventory.EnsureExists();
        for (int i = 0; i < types.Length; i++)
            lastBackpack[i] = inv.GetBackpackCount(types[i]);
        inv.OnChanged += OnInventoryChanged;
        FindPlayer();
    }

    private void OnDestroy()
    {
        if (GameInventory.Instance != null)
            GameInventory.Instance.OnChanged -= OnInventoryChanged;
    }

    private void FindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;

        health = go.GetComponentInChildren<Health>();
        if (health == null) health = go.GetComponentInParent<Health>();

        shooter = go.GetComponentInChildren<PlayerShooter>();
        if (shooter == null) shooter = go.GetComponentInParent<PlayerShooter>();
    }

    private void Update()
    {
        if ((health == null || shooter == null) && Time.time >= nextFindTime)
        {
            nextFindTime = Time.time + 0.5f;
            FindPlayer();
        }

        RefreshStatus();
        UpdateToasts();
    }

    private void RefreshStatus()
    {
        if (health != null)
        {
            float ratio = health.MaxHealth > 0f ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth) : 0f;
            hpFill.anchorMax = new Vector2(ratio, 1f);
            hpFillImage.color = Color.Lerp(Red, Green, ratio);
            hpText.text = health.IsDead
                ? "KIA"
                : "HP  " + Mathf.CeilToInt(health.CurrentHealth) + " / " + Mathf.CeilToInt(health.MaxHealth);
        }
        else
        {
            hpText.text = "HP  --";
        }

        if (shooter != null)
        {
            if (shooter.IsReloading)
            {
                ammoText.text = "RELOADING...";
                ammoText.color = new Color(1f, 0.85f, 0.4f);
            }
            else
            {
                string scope = shooter.ScopeAccuracyMultiplier > 1.01f
                    ? "  [" + shooter.ScopeAccuracyMultiplier.ToString("F1") + "x]"
                    : "  [IRON]";
                ammoText.text = "AMMO  " + shooter.CurrentAmmo + " / " + shooter.ReserveAmmo + scope;
                ammoText.color = shooter.CurrentAmmo == 0 ? Red : Color.white;
            }
        }
        else
        {
            ammoText.text = "AMMO  --";
        }

        GameInventory inv = GameInventory.Instance;
        if (inv != null)
        {
            carryText.text = SceneManager.GetActiveScene().name == "_Base"
                ? "MONEY  $" + inv.Money
                : "BACKPACK  " + inv.GetBackpackTotal() + " / " + inv.BackpackCapacity;
        }
    }

    private void OnInventoryChanged()
    {
        GameInventory inv = GameInventory.Instance;
        if (inv == null) return;

        for (int i = 0; i < types.Length; i++)
        {
            int now = inv.GetBackpackCount(types[i]);
            if (now > lastBackpack[i])
                AddToast("+" + (now - lastBackpack[i]) + "  " + ItemInfo.GetName(types[i]));
            lastBackpack[i] = now;
        }
    }

    private void AddToast(string message)
    {
        Image bg = NewImage("Toast", toastRoot, new Color(0.08f, 0.08f, 0.1f, 0.9f));
        SetSize(bg.gameObject, 300, 42);

        Text t = NewText("Text", bg.transform, 24, TextAnchor.MiddleCenter, true);
        t.text = message;
        t.color = Green;
        Stretch(t.rectTransform);

        toasts.Add(new Toast { bg = bg, text = t, expire = Time.time + ToastDuration });
    }

    private void UpdateToasts()
    {
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            float remaining = toasts[i].expire - Time.time;
            if (remaining <= 0f)
            {
                Destroy(toasts[i].bg.gameObject);
                toasts.RemoveAt(i);
                continue;
            }

            float alpha = remaining < FadeTime ? remaining / FadeTime : 1f;
            Color bgc = toasts[i].bg.color;
            bgc.a = 0.9f * alpha;
            toasts[i].bg.color = bgc;
            Color tc = toasts[i].text.color;
            tc.a = alpha;
            toasts[i].text.color = tc;
        }
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("CombatCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        Image panel = NewImage("StatusPanel", canvasGO.transform, new Color(0.05f, 0.05f, 0.07f, 0.88f));
        RectTransform prt = panel.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0f, 0f);
        prt.pivot = new Vector2(0f, 0f);
        prt.anchoredPosition = new Vector2(24f, 24f);
        prt.sizeDelta = new Vector2(380f, 100f);

        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 6;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image bar = NewImage("HpBar", panel.transform, new Color(0.2f, 0.2f, 0.22f, 1f));
        SetSize(bar.gameObject, -1, 36);

        hpFillImage = NewImage("HpFill", bar.transform, Green);
        hpFill = hpFillImage.rectTransform;
        hpFill.anchorMin = new Vector2(0f, 0f);
        hpFill.anchorMax = new Vector2(1f, 1f);
        hpFill.offsetMin = Vector2.zero;
        hpFill.offsetMax = Vector2.zero;

        hpText = NewText("HpText", bar.transform, 22, TextAnchor.MiddleCenter, true);
        Stretch(hpText.rectTransform);

        ammoText = NewText("Ammo", panel.transform, 26, TextAnchor.MiddleLeft, true);
        SetSize(ammoText.gameObject, -1, 34);

        carryText = NewText("Carry", panel.transform, 22, TextAnchor.MiddleLeft, false);
        carryText.color = new Color(1f, 0.85f, 0.4f);
        SetSize(carryText.gameObject, -1, 30);

        toastRoot = NewRect("Toasts", canvasGO.transform);
        toastRoot.anchorMin = toastRoot.anchorMax = new Vector2(1f, 1f);
        toastRoot.pivot = new Vector2(1f, 1f);
        toastRoot.anchoredPosition = new Vector2(-24f, -96f);
        toastRoot.sizeDelta = new Vector2(300f, 10f);

        var tvlg = toastRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        tvlg.spacing = 6;
        tvlg.childAlignment = TextAnchor.UpperRight;
        tvlg.childControlWidth = true;
        tvlg.childControlHeight = true;
        tvlg.childForceExpandWidth = false;
        tvlg.childForceExpandHeight = false;

        var tfitter = toastRoot.gameObject.AddComponent<ContentSizeFitter>();
        tfitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        tfitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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