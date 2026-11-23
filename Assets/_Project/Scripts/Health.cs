using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum HitZone
{
    Miss,
    Head,
    Torso,
    Arms,
    Legs
}

public class Health : MonoBehaviour
{
    [Header("Base Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool isPlayer = false;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 0.1f;

    [Header("Helmet (Data Only - No 3D Model Yet)")]
    [SerializeField] private bool hasHelmet = false;
    [Range(0f, 1f)]
    [SerializeField] private float helmetSaveChance = 0.45f;
    [SerializeField] private float helmetBluntDamage = 35f;

    [Header("Body Armor (Data Only - No 3D Model Yet)")]
    [SerializeField] private bool hasBodyArmor = false;
    [Range(0f, 0.9f)]
    [SerializeField] private float bodyArmorReduction = 0.40f;

    [Header("Limb Multipliers")]
    [SerializeField] private float limbDamageMultiplier = 0.65f;

    [Header("Prototype HUD & Hit Log")]
    [SerializeField] private bool showPlayerHealthHUD = true;
    [SerializeField] private bool showLastHitHUD = true;

    [Header("Events")]
    public UnityEvent<float, float> onHealthChanged;
    public UnityEvent onDeath;

    private float currentHealth;
    private bool isDead;

    private static string globalLastHitText = "";
    private static float globalLastHitExpireTime = 0f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool HasHelmet => hasHelmet;
    public bool HasBodyArmor => hasBodyArmor;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ReceiveBulletHit(float baseDamage, HitZone zone)
    {
        if (isDead || zone == HitZone.Miss) return;

        float finalDamage = 0f;
        string feedback = "";

        switch (zone)
        {
            case HitZone.Head:
                if (hasHelmet && Random.value <= helmetSaveChance)
                {
                    finalDamage = helmetBluntDamage;
                    feedback = $"HELMET SAVED! (-{Mathf.CeilToInt(finalDamage)} HP)";
                }
                else
                {
                    finalDamage = maxHealth * 10f;
                    feedback = "HEADSHOT! (INSTANT KILL)";
                }
                break;

            case HitZone.Torso:
                if (hasBodyArmor)
                {
                    finalDamage = baseDamage * (1f - bodyArmorReduction);
                    feedback = $"TORSO [ARMORED] (-{Mathf.CeilToInt(finalDamage)} HP)";
                }
                else
                {
                    finalDamage = baseDamage;
                    feedback = $"TORSO (-{Mathf.CeilToInt(finalDamage)} HP)";
                }
                break;

            case HitZone.Arms:
                finalDamage = baseDamage * limbDamageMultiplier;
                feedback = $"ARMS (-{Mathf.CeilToInt(finalDamage)} HP)";
                break;

            case HitZone.Legs:
                finalDamage = baseDamage * limbDamageMultiplier;
                feedback = $"LEGS (-{Mathf.CeilToInt(finalDamage)} HP)";
                break;
        }

        SetHitFeedback($"{gameObject.name}: {feedback}");
        TakeDamage(finalDamage);
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void EquipHelmet(bool equipped, float saveChance = 0.45f)
    {
        hasHelmet = equipped;
        helmetSaveChance = Mathf.Clamp01(saveChance);
    }

    public void EquipBodyArmor(bool equipped, float reduction = 0.40f)
    {
        hasBodyArmor = equipped;
        bodyArmorReduction = Mathf.Clamp(reduction, 0f, 0.9f);
    }

    public static void SetHitFeedback(string message)
    {
        globalLastHitText = message;
        globalLastHitExpireTime = Time.time + 2.2f;
        Debug.Log("[Combat] " + message);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        onDeath?.Invoke();

        if (isPlayer)
        {
            GameInventory.EnsureExists().LoseRaidLootOnDeath();
            Invoke(nameof(ReturnToBaseAfterDeath), 1.5f);
            return;
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void ReturnToBaseAfterDeath()
    {
        if (Application.CanStreamedLevelBeLoaded("_Base"))
        {
            SceneManager.LoadScene("_Base");
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnGUI()
    {
        if (!isPlayer) return;

        if (showPlayerHealthHUD)
        {
            GUIStyle hpStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            string status = isDead ? "KIA — LOOT LOST" : $"HP: {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
            GUI.Box(new Rect(20, 20, 220, 36), "  " + status, hpStyle);
        }

        if (showLastHitHUD && Time.time < globalLastHitExpireTime && !string.IsNullOrEmpty(globalLastHitText))
        {
            GUIStyle hitStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            GUI.Box(new Rect(20, 104, 340, 32), "  " + globalLastHitText, hitStyle);
        }
    }
}