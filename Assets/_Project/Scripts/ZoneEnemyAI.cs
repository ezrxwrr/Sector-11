using System.Collections;
using UnityEngine;

public class ZoneEnemyAI : MonoBehaviour
{
    [Header("Zone Patrol Settings")]
    [SerializeField] private float zoneRadius = 5.5f;
    [SerializeField] private float patrolSpeed = 2.4f;
    [SerializeField] private float combatStrafeSpeed = 2.6f;
    [SerializeField] private float waitTimeAtPoint = 1.0f;
    [SerializeField] private float obstacleCheckRadius = 0.45f;

    [Header("Detection & Tactical Burst Combat")]
    [SerializeField] private float aggroRange = 15f;
    [SerializeField] private float rotationSpeed = 11f;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float timeBetweenBurstShots = 0.16f;
    [SerializeField] private float burstCooldown = 1.4f;
    [SerializeField] private float baseDamage = 15f;
    [SerializeField] private float eyeHeight = 0.45f;

    [Header("Enemy Hit Chance Weights (%)")]
    [SerializeField] private float missWeight = 38f;
    [SerializeField] private float headWeight = 2f;
    [SerializeField] private float torsoWeight = 35f;
    [SerializeField] private float armsWeight = 12.5f;
    [SerializeField] private float legsWeight = 12.5f;

    [Header("Tracer Visual")]
    [SerializeField] private bool showBulletTracer = true;
    [SerializeField] private float tracerDuration = 0.045f;
    [SerializeField] private Color tracerColor = new Color(1f, 0.25f, 0.2f, 1f);

    [Header("Alert Allies")]
    [SerializeField] private float alertAlliesRadius = 20f;
    [SerializeField] private float alertCooldown = 5f;

    private Vector3 homePosition;
    private Vector3 currentDestination;
    private float waitTimer;
    private float nextBurstTime;
    private bool isFiringBurst;
    private float lastKnownHealth;
    private float alertTimer;

    private Transform playerTransform;
    private Health playerHealth;
    private Health myHealth;
    private LineRenderer tracerLine;
    private bool hasAlertedAllies;
    private float alertCooldownTimer;

    private void Awake()
    {
        myHealth = GetComponent<Health>();
        SetupTracerLine();
    }

    private void Start()
    {
        homePosition = transform.position;
        if (myHealth != null) lastKnownHealth = myHealth.CurrentHealth;
        FindPlayer();
        PickNewZonePoint();
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponentInParent<Health>();
        }
    }

    private void SetupTracerLine()
    {
        if (!showBulletTracer) return;

        GameObject tracerObj = new GameObject("EnemyBulletTracer");
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
        if (myHealth != null && myHealth.IsDead) return;

        if (alertTimer > 0f)
        {
            alertTimer -= Time.deltaTime;
        }

        if (alertCooldownTimer > 0f)
        {
            alertCooldownTimer -= Time.deltaTime;
        }

        // Evasive reaction when taking damage
        if (myHealth != null && myHealth.CurrentHealth < lastKnownHealth)
        {
            lastKnownHealth = myHealth.CurrentHealth;
            waitTimer = 0f;
            alertTimer = 4f;
            PickNewZonePoint();
        }

        if (playerTransform == null) FindPlayer();

        bool canSeePlayer = CheckPlayerInSight();

        if (canSeePlayer && (playerHealth == null || !playerHealth.IsDead))
        {
            HandleCombatState();
        }
        else
        {
            HandlePatrolState();
        }
    }

    private bool CheckPlayerInSight()
    {
        if (playerTransform == null) return false;

        float effectiveRange = alertTimer > 0f ? aggroRange * 1.35f : aggroRange;
        Vector3 myEye = transform.position + Vector3.up * eyeHeight;
        Vector3 playerChest = playerTransform.position + Vector3.up * 1.1f;
        Vector3 toPlayer = playerChest - myEye;
        float distance = toPlayer.magnitude;

        if (distance > effectiveRange) return false;

        if (Physics.Raycast(myEye, toPlayer.normalized, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.root != playerTransform.root) return false;
        }

        return true;
    }

    private void HandlePatrolState()
    {
        hasAlertedAllies = false;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        MoveTowardDestination(patrolSpeed, faceMoveDirection: true);
    }

    private void HandleCombatState()
    {
        // Alert nearby allies when first spotting the player
        if (!hasAlertedAllies && alertCooldownTimer <= 0f)
        {
            AlertNearbyAllies();
            hasAlertedAllies = true;
            alertCooldownTimer = alertCooldown;
        }

        Vector3 lookDir = playerTransform.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        MoveTowardDestination(combatStrafeSpeed, faceMoveDirection: false);

        if (!isFiringBurst && Time.time >= nextBurstTime)
        {
            StartCoroutine(FireBurstCoroutine());
        }
    }

    private IEnumerator FireBurstCoroutine()
    {
        isFiringBurst = true;

        for (int i = 0; i < burstCount; i++)
        {
            if (myHealth != null && myHealth.IsDead) break;
            if (!CheckPlayerInSight()) break;

            ShootAtPlayer();
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }

        nextBurstTime = Time.time + burstCooldown + Random.Range(-0.2f, 0.35f);
        isFiringBurst = false;
    }

    private void MoveTowardDestination(float speed, bool faceMoveDirection)
    {
        Vector3 flatTarget = new Vector3(currentDestination.x, transform.position.y, currentDestination.z);
        Vector3 toDest = flatTarget - transform.position;
        float dist = toDest.magnitude;

        if (dist <= 0.25f)
        {
            waitTimer = waitTimeAtPoint;
            PickNewZonePoint();
            return;
        }

        Vector3 moveDir = toDest.normalized;
        float step = speed * Time.deltaTime;

        if (faceMoveDirection)
        {
            Quaternion moveRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRot, rotationSpeed * Time.deltaTime);
        }

        Vector3 castOrigin = transform.position + Vector3.up * 0.5f;
        if (Physics.SphereCast(castOrigin, obstacleCheckRadius, moveDir, out RaycastHit wallHit, step + 0.25f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (wallHit.transform.root != transform.root)
            {
                PickNewZonePoint();
                return;
            }
        }

        transform.position += moveDir * step;
    }

    private void PickNewZonePoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * zoneRadius;
        currentDestination = new Vector3(
            homePosition.x + randomCircle.x,
            transform.position.y,
            homePosition.z + randomCircle.y
        );
    }

    private void AlertNearbyAllies()
    {
        ZoneEnemyAI[] allEnemies = FindObjectsByType<ZoneEnemyAI>(FindObjectsSortMode.None);
        foreach (ZoneEnemyAI ally in allEnemies)
        {
            if (ally == this) continue;
            if (ally.myHealth != null && ally.myHealth.IsDead) continue;

            float dist = Vector3.Distance(transform.position, ally.transform.position);
            if (dist <= alertAlliesRadius)
            {
                ally.ReceiveAlert(playerTransform.position);
            }
        }
    }

    public void ReceiveAlert(Vector3 threatPosition)
    {
        if (myHealth != null && myHealth.IsDead) return;
        if (alertTimer > 0f) return; // Already alert

        alertTimer = 4f;
        waitTimer = 0f;
        currentDestination = threatPosition;
    }

    private void ShootAtPlayer()
    {
        if (playerTransform == null) return;

        Vector3 origin = transform.position + Vector3.up * eyeHeight + transform.forward * 0.55f;
        Vector3 targetChest = playerTransform.position + Vector3.up * 1.1f;
        Vector3 shootDir = (targetChest - origin).normalized;
        Vector3 endPoint = targetChest;

        HitZone rolledZone = RollHitZone();

        if (rolledZone == HitZone.Miss)
        {
            Vector3 sideOffset = Vector3.Cross(Vector3.up, shootDir).normalized;
            float sideSign = Random.value < 0.5f ? -1f : 1f;
            endPoint += sideOffset * (sideSign * Random.Range(0.6f, 1.0f)) + shootDir * 1.5f;
        }
        else if (playerHealth != null)
        {
            playerHealth.ReceiveBulletHit(baseDamage, rolledZone);
        }

        if (showBulletTracer && tracerLine != null)
        {
            StopCoroutine(nameof(ShowTracerCoroutine));
            StartCoroutine(ShowTracerCoroutine(origin, endPoint));
        }
    }

    private HitZone RollHitZone()
    {
        float total = Mathf.Max(0.01f, missWeight + headWeight + torsoWeight + armsWeight + legsWeight);
        float roll = Random.Range(0f, total);

        if (roll < missWeight) return HitZone.Miss;
        roll -= missWeight;

        if (roll < headWeight) return HitZone.Head;
        roll -= headWeight;

        if (roll < torsoWeight) return HitZone.Torso;
        roll -= torsoWeight;

        if (roll < armsWeight) return HitZone.Arms;

        return HitZone.Legs;
    }

    private IEnumerator ShowTracerCoroutine(Vector3 start, Vector3 end)
    {
        tracerLine.SetPosition(0, start);
        tracerLine.SetPosition(1, end);
        tracerLine.enabled = true;

        yield return new WaitForSeconds(tracerDuration);

        tracerLine.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, zoneRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, alertAlliesRadius);
    }
}
