using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerShooter : MonoBehaviour
{
    [Header("Weapon Base Stats")]
    [SerializeField] private float baseDamage = 35f;
    [SerializeField] private float fireRate = 8f;
    [SerializeField] private float maxRange = 45f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Barebone Hit Chance Weights (%)")]
    [Tooltip("Peluang peluru meleset total pada senjata barebone.")]
    [SerializeField] private float baseMissWeight = 15f;
    [Tooltip("Peluang headshot pada senjata barebone (rendah).")]
    [SerializeField] private float baseHeadWeight = 5f;
    [Tooltip("Peluang kena badan/torso pada senjata barebone (paling tinggi).")]
    [SerializeField] private float baseTorsoWeight = 50f;
    [Tooltip("Peluang kena tangan (sama dengan kaki).")]
    [SerializeField] private float baseArmsWeight = 15f;
    [Tooltip("Peluang kena kaki (sama dengan tangan).")]
    [SerializeField] private float baseLegsWeight = 15f;

    [Header("Optics / Scope Multiplier")]
    [Tooltip("1.0 = Barebone/Iron Sight. Naikkan (mis. 1.5 = Red Dot, 2.5 = 4x Scope) untuk memperkecil Miss & menaikkan Head/Torso.")]
    [SerializeField] private float scopeAccuracyMultiplier = 1.0f;
    [SerializeField] private float maxHeadChancePercent = 35f;
    [Tooltip("Jarak dekat (meter) di mana peluang Miss menjadi 0%.")]
    [SerializeField] private float pointBlankRange = 2.5f;

    [Header("Ammo & Reload")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int reserveAmmo = 120;
    [SerializeField] private float reloadDuration = 1.6f;

    [Header("References")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float aimHeight = 1.3f;

    [Header("Tracer Visual")]
    [SerializeField] private bool showBulletTracer = true;
    [SerializeField] private float tracerDuration = 0.045f;
    [SerializeField] private Color tracerColor = new Color(1f, 0.85f, 0.3f, 1f);

    [Header("Animation")]
    [Tooltip("Isi sesuai nama parameter di PlayerAnimator (otomatis mendeteksi Trigger maupun Bool).")]
    [SerializeField] private string shootParameterName = "Shoot";

    [Header("Prototype HUD")]
    [SerializeField] private bool showAmmoHUD = true;

    private int currentAmmo;
    private bool isReloading;
    private float nextFireTime;
    private Animator animator;
    private LineRenderer tracerLine;

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;
    public float ScopeAccuracyMultiplier => scopeAccuracyMultiplier;

    private void Awake()
    {
        currentAmmo = magazineSize;
        animator = GetComponentInChildren<Animator>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        SetupTracerLine();
    }

    private void SetupTracerLine()
    {
        if (!showBulletTracer) return;

        GameObject tracerObj = new GameObject("BulletTracer");
        tracerObj.transform.SetParent(transform);
        tracerLine = tracerObj.AddComponent<LineRenderer>();
        tracerLine.positionCount = 2;
        tracerLine.startWidth = 0.035f;
        tracerLine.endWidth = 0.015f;
        tracerLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tracerLine.receiveShadows = false;
        tracerLine.enabled = false;

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Sprites/Default");
        Material mat = new Material(unlit);
        mat.color = tracerColor;
        tracerLine.material = mat;
    }

    private void Update()
    {
        bool fireHeld = ReadFireHeld();

        // Sinkronisasi otomatis jika parameter di Animator bertipe Bool
        UpdateAnimatorBoolIfUsed(fireHeld && !isReloading && currentAmmo > 0);

        if (isReloading) return;

        if (ReadReloadPressed() && currentAmmo < magazineSize && reserveAmmo > 0)
        {
            StartCoroutine(ReloadCoroutine());
            return;
        }

        if (fireHeld && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                nextFireTime = Time.time + (1f / Mathf.Max(0.1f, fireRate));
                Shoot();
            }
            else if (reserveAmmo > 0)
            {
                StartCoroutine(ReloadCoroutine());
            }
        }
    }

    private void Shoot()
    {
        currentAmmo--;

        TriggerAnimatorShootIfUsed();

        Vector3 origin = muzzlePoint != null
            ? muzzlePoint.position
            : transform.position + Vector3.up * aimHeight;

        Vector3 shootDir = GetAimDirection(origin);
        Vector3 endPoint = origin + shootDir * maxRange;

        if (Physics.Raycast(origin, shootDir, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.root != transform.root)
            {
                endPoint = hit.point;

                Health targetHealth = hit.collider.GetComponentInParent<Health>();
                if (targetHealth != null)
                {
                    HitZone rolledZone = RollHitZone(hit.distance);

                    if (rolledZone == HitZone.Miss)
                    {
                        Vector3 sideOffset = Vector3.Cross(Vector3.up, shootDir).normalized;
                        float sideSign = Random.value < 0.5f ? -1f : 1f;
                        endPoint += sideOffset * (sideSign * Random.Range(0.55f, 0.9f)) + shootDir * 1.5f;

                        Health.SetHitFeedback($"{targetHealth.gameObject.name}: MISS");
                    }
                    else
                    {
                        targetHealth.ReceiveBulletHit(baseDamage, rolledZone);
                    }
                }
            }
        }

        if (showBulletTracer && tracerLine != null)
        {
            StopCoroutine(nameof(ShowTracerCoroutine));
            StartCoroutine(ShowTracerCoroutine(origin, endPoint));
        }
    }

    private void TriggerAnimatorShootIfUsed()
    {
        if (animator == null || string.IsNullOrEmpty(shootParameterName)) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == shootParameterName && param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(shootParameterName);
                return;
            }
        }
    }

    private void UpdateAnimatorBoolIfUsed(bool isFiring)
    {
        if (animator == null || string.IsNullOrEmpty(shootParameterName)) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == shootParameterName && param.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(shootParameterName, isFiring);
                return;
            }
        }
    }

    private HitZone RollHitZone(float distanceToTarget)
    {
        float mult = Mathf.Max(0.25f, scopeAccuracyMultiplier);

        float miss = distanceToTarget <= pointBlankRange ? 0f : (baseMissWeight / mult);
        float head = Mathf.Min(maxHeadChancePercent, baseHeadWeight * mult);

        float remaining = Mathf.Max(0f, 100f - (miss + head));
        float limbBaseSum = Mathf.Max(0.01f, baseTorsoWeight + baseArmsWeight + baseLegsWeight);

        float torso = remaining * (baseTorsoWeight / limbBaseSum);
        float arms = remaining * (baseArmsWeight / limbBaseSum);
        float legs = remaining * (baseLegsWeight / limbBaseSum);

        float roll = Random.Range(0f, miss + head + torso + arms + legs);

        if (roll < miss) return HitZone.Miss;
        roll -= miss;

        if (roll < head) return HitZone.Head;
        roll -= head;

        if (roll < torso) return HitZone.Torso;
        roll -= torso;

        if (roll < arms) return HitZone.Arms;

        return HitZone.Legs;
    }

    public void SetScopeMultiplier(float newMultiplier)
    {
        scopeAccuracyMultiplier = Mathf.Max(0.25f, newMultiplier);
    }

    private Vector3 GetAimDirection(Vector3 origin)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return transform.forward;
        }

        Vector2 mousePos = ReadMouseScreenPosition();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        float planeY = transform.position.y + aimHeight;
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

        if (aimPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 targetPoint = ray.GetPoint(enterDistance);
            Vector3 flatDir = targetPoint - origin;
            flatDir.y = 0f;

            if (flatDir.sqrMagnitude > 0.001f)
            {
                return flatDir.normalized;
            }
        }

        return transform.forward;
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadDuration);

        int needed = magazineSize - currentAmmo;
        int toLoad = Mathf.Min(needed, reserveAmmo);
        currentAmmo += toLoad;
        reserveAmmo -= toLoad;

        isReloading = false;
    }

    private IEnumerator ShowTracerCoroutine(Vector3 start, Vector3 end)
    {
        tracerLine.SetPosition(0, start);
        tracerLine.SetPosition(1, end);
        tracerLine.enabled = true;

        yield return new WaitForSeconds(tracerDuration);

        tracerLine.enabled = false;
    }

    private bool ReadFireHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    private bool ReadReloadPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    private Vector2 ReadMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    private void OnGUI()
    {
        if (!showAmmoHUD) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        string scopeTag = scopeAccuracyMultiplier > 1.01f ? $" [{scopeAccuracyMultiplier:F1}x]" : " [IRON]";
        string text = isReloading ? "RELOADING..." : $"AMMO: {currentAmmo}/{reserveAmmo}{scopeTag}";
        GUI.Box(new Rect(20, 62, 220, 36), "  " + text, style);
    }
}