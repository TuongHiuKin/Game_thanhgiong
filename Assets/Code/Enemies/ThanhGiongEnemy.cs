using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class ThanhGiongEnemy : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chasing,
        TelegraphingAttack,
        Slamming,
        StuckInGround,    // Cửa sổ sơ hở (Vulnerability Window) - lấy cảm hứng từ mudLodge của The Heron trong Understory!
        Recovering,
        Stunned,
        Dead
    }

    [Header("Enemy Attributes")]
    public float maxHealth = 55f;
    public float moveSpeed = 2.4f;
    public float engageDistance = 80f;
    public bool isBoss;
    public string enemyName = "Giáo Binh Ân";

    [Header("Heavy Slam & Vulnerability (Understory Mechanics)")]
    public float heavyAttackRange = 4.2f;
    public float heavyAttackCooldown = 5.5f;
    public float telegraphDuration = 0.85f;
    public float stuckDuration = 1.5f;           // Đại đao găm đất đúng 1,5 giây
    public float vulnerabilityMultiplier = 2.0f; // Nhận gấp đôi sát thương khi đang mắc kẹt!
    public float slamDamage = 35f;
    public float slamRadius = 2.8f;
    public float minionAttackInterval = 2.0f;
    public float locomotionSmoothTime = 0.16f;

    [Header("Iron Horse Pursuit & Formation")]
    public float surroundRadius = 2.6f;
    public float formationTolerance = 0.65f;
    public float separationRadius = 1.1f;
    public float separationStrength = 1.35f;

    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;
    public bool IsStuckInGround => CurrentState == EnemyState.StuckInGround;
    public bool IsStunned => Time.time < stunnedUntil || CurrentState == EnemyState.Stunned;
    public bool IsRooted => Time.time < rootedUntil;
    public float HealthRatio => Mathf.Clamp01(health / maxHealth);
    public Rigidbody Body => rb;

    private float health;
    private float stunnedUntil;
    private float rootedUntil;
    private Transform target;
    private Transform focusTarget;
    private ThanhGiongCampaignController campaign;
    private Vector3 initialScale;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Rigidbody rb;
    private CapsuleCollider col;
    private ThanhGiongRagdollPhysics ragdoll;

    private float nextHeavyAttackTime;
    private float nextMinionAttackTime;
    private float stateTimer;
    private Vector3 slamTargetPos;
    private GameObject activeWarningRing;
    private GameObject stuckBladeMarker;
    private Transform visualModel;
    private Vector3 visualBaseLocalPos;
    private Quaternion visualBaseLocalRot;
    private Vector3 smoothedPlanarVelocity;
    private Vector3 velocityDamp;
    private Vector3 visualBaseScale;
    private float previousYaw;
    private float currentBankAngle;
    private float enemyGaitTime;
    private float flinchTimer;
    private float visualSpeedBlend;
    private float visualSpeedBlendVel;
    private float attackPoseBlend;
    private float stuckPoseBlend;
    private float stunnedPoseBlend;
    private Vector3 visualPoseVelocity;
    private Vector3 visualScaleVelocity;
    private Vector3 formationOffset;
    private int formationSlot = -1;
    private static int nextFormationSlot;
    private readonly Collider[] separationHits = new Collider[16];

    public Transform FocusTarget => focusTarget;

    private void Awake()
    {
        initialScale = transform.localScale;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        if (isBoss)
        {
            maxHealth = 480f;
            moveSpeed = 3.0f;
            enemyName = "Tướng Giặc Ân";
            transform.localScale = initialScale * 1.8f;
        }

        col = GetComponent<CapsuleCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<CapsuleCollider>();
            col.radius = isBoss ? 0.9f : 0.5f;
            col.height = isBoss ? 3.6f : 2.0f;
            col.center = Vector3.zero;
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.mass = isBoss ? 180f : 80f;
        rb.linearDamping = 0.8f;
        rb.angularDamping = 2.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.isKinematic = false;

        ragdoll = GetComponent<ThanhGiongRagdollPhysics>();
        if (ragdoll == null) ragdoll = gameObject.AddComponent<ThanhGiongRagdollPhysics>();

        ThanhGiongEnemyAnimationDriver animationDriver = GetComponent<ThanhGiongEnemyAnimationDriver>();
        if (animationDriver == null) gameObject.AddComponent<ThanhGiongEnemyAnimationDriver>();

        // Cache or create visual pivot for procedural combat telegraphing & struggle shaking
        CacheVisualPivot();
        ResolveIronHorseTarget();
    }

    private void CacheVisualPivot()
    {
        if (transform.childCount > 0)
        {
            visualModel = transform.Find("Visual");
            if (visualModel == null) visualModel = transform.GetChild(0);
        }
        else
        {
            visualModel = transform;
        }

        if (visualModel != null && visualModel != transform)
        {
            visualBaseLocalPos = visualModel.localPosition;
            visualBaseLocalRot = visualModel.localRotation;
            visualBaseScale = visualModel.localScale;
            previousYaw = transform.eulerAngles.y;
        }
    }

    private void OnEnable()
    {
        if (formationSlot < 0)
        {
            formationSlot = nextFormationSlot++;
            ComputeFormationOffset();
        }

        ResolveIronHorseTarget();
        health = maxHealth;
        stunnedUntil = 0f;
        rootedUntil = 0f;
        CurrentState = EnemyState.Idle;
        nextHeavyAttackTime = Time.time + Random.Range(1.5f, 3.0f);
        nextMinionAttackTime = Time.time + Random.Range(0.5f, 1.5f);
        smoothedPlanarVelocity = Vector3.zero;
        velocityDamp = Vector3.zero;
        visualSpeedBlend = 0f;
        visualSpeedBlendVel = 0f;
        attackPoseBlend = 0f;
        stuckPoseBlend = 0f;
        stunnedPoseBlend = 0f;
        visualPoseVelocity = Vector3.zero;
        visualScaleVelocity = Vector3.zero;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ResetVisualPose();
    }

    private void OnDisable()
    {
        DestroyWarningRing();
        DestroyStuckBladeMarker();
    }

    public void Initialize(ThanhGiongCampaignController owner, Transform player)
    {
        campaign = owner;
        target = player;
        ResolveIronHorseTarget();
        gameObject.SetActive(true);
        CurrentState = EnemyState.Chasing;
    }

    public void ResetForBattle(ThanhGiongCampaignController owner, Transform player)
    {
        StopAllCoroutines();
        if (ragdoll != null) ragdoll.ResetForBattle();
        gameObject.SetActive(false); // Restores original dissolve materials and clears telegraphs.
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        transform.localScale = isBoss ? initialScale * 1.8f : initialScale;
        if (col != null) col.enabled = true;
        stateTimer = 0; flinchTimer = 0; enemyGaitTime = 0;
        visualSpeedBlend = 0f; visualSpeedBlendVel = 0f; attackPoseBlend = 0f; stuckPoseBlend = 0f; stunnedPoseBlend = 0f;
        visualPoseVelocity = Vector3.zero; visualScaleVelocity = Vector3.zero;
        foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true)) {
            var main = particles.main; main.playOnAwake = false;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        campaign = owner; target = player;
        gameObject.SetActive(true);
        Initialize(owner, player);
    }

    private void Update()
    {
        if (target == null || focusTarget == null) ResolveIronHorseTarget();

        AnimateEnemyProcedural();
    }

    private void AnimateEnemyProcedural()
    {
        if (visualModel == null || visualModel == transform || CurrentState == EnemyState.Dead) return;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 planarVelocity = rb != null ? new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) : Vector3.zero;
        float speed = planarVelocity.magnitude;
        float desiredSpeedBlend = CurrentState == EnemyState.Chasing ? Mathf.Clamp01(speed / Mathf.Max(moveSpeed, 0.01f)) : 0f;
        visualSpeedBlend = Mathf.SmoothDamp(visualSpeedBlend, desiredSpeedBlend, ref visualSpeedBlendVel, CurrentState == EnemyState.Chasing ? 0.16f : 0.24f, 8f, dt);

        // Dynamic turn banking calculation (Understory predator steering feel)
        float yawDelta = Mathf.DeltaAngle(previousYaw, transform.eulerAngles.y) / dt;
        previousYaw = transform.eulerAngles.y;
        float targetBank = -Mathf.Clamp(yawDelta * 0.22f, -10f, 10f) * Mathf.Lerp(.35f, 1f, visualSpeedBlend);
        currentBankAngle = Mathf.Lerp(currentBankAngle, targetBank, 1f - Mathf.Exp(-9f * dt));
        attackPoseBlend = Mathf.MoveTowards(attackPoseBlend, CurrentState == EnemyState.TelegraphingAttack || CurrentState == EnemyState.Slamming ? 1f : 0f, dt * 5.5f);
        stuckPoseBlend = Mathf.MoveTowards(stuckPoseBlend, CurrentState == EnemyState.StuckInGround || CurrentState == EnemyState.Recovering ? 1f : 0f, dt * 4f);
        stunnedPoseBlend = Mathf.MoveTowards(stunnedPoseBlend, CurrentState == EnemyState.Stunned ? 1f : 0f, dt * 5f);

        Vector3 targetPos = visualBaseLocalPos;
        Quaternion targetRot = visualBaseLocalRot;
        Vector3 targetScale = visualBaseScale;

        // Flinch reaction when struck
        if (flinchTimer > 0f)
        {
            flinchTimer -= Time.deltaTime;
            float flinchT = flinchTimer / 0.16f;
            targetScale = new Vector3(visualBaseScale.x * (1f + flinchT * 0.09f), visualBaseScale.y * (1f - flinchT * 0.12f), visualBaseScale.z * (1f + flinchT * 0.09f));
            targetPos += Random.insideUnitSphere * (flinchT * 0.08f);
        }

        switch (CurrentState)
        {
            case EnemyState.Idle:
                float breatheRate = isBoss ? 1.8f : 2.6f;
                float breathe = Mathf.Sin(Time.time * breatheRate);
                targetPos.y += breathe * 0.024f;
                targetRot *= Quaternion.Euler(breathe * 1.4f, Mathf.Cos(Time.time * 0.9f) * 2.2f, 0f);
                targetScale = new Vector3(visualBaseScale.x * (1f - breathe * 0.015f), visualBaseScale.y * (1f + breathe * 0.022f), visualBaseScale.z * (1f - breathe * 0.015f));
                break;

            case EnemyState.Chasing:
                enemyGaitTime += dt * (isBoss ? 5.2f : 7.4f) * (0.35f + visualSpeedBlend * 0.65f);

                float wave = Mathf.Sin(enemyGaitTime);
                float doubleWave = Mathf.Sin(enemyGaitTime * 2f);
                float footPlant = Mathf.Pow(Mathf.Abs(wave), 0.65f);
                float stepLift = Mathf.Max(0f, Mathf.Sin(enemyGaitTime + Mathf.PI * .22f));

                // Heavy footstep vertical bob
                float stepBob = footPlant * (isBoss ? 0.085f : 0.052f) * visualSpeedBlend;
                targetPos.y += stepBob + stepLift * (isBoss ? 0.018f : 0.026f) * visualSpeedBlend;
                if (planarVelocity.sqrMagnitude > .01f && visualModel.parent != null)
                    targetPos += visualModel.parent.InverseTransformDirection(planarVelocity.normalized) * (Mathf.Sin(enemyGaitTime + Mathf.PI) * .035f * visualSpeedBlend);

                // Predatory forward lean while hunting
                float forwardPitch = (isBoss ? 9f : 12f) * visualSpeedBlend;

                // Heavy waddle sway (boss steps side to side with immense mass)
                float waddleRoll = wave * (isBoss ? 6.2f : 3.2f) * visualSpeedBlend;

                targetRot *= Quaternion.Euler(
                    forwardPitch + doubleWave * 2.2f,
                    Mathf.Sin(enemyGaitTime * .5f) * (isBoss ? 1.4f : 2.1f) * visualSpeedBlend,
                    waddleRoll + currentBankAngle
                );

                targetScale = new Vector3(
                    visualBaseScale.x * (1f + wave * 0.018f * visualSpeedBlend),
                    visualBaseScale.y * (1f - footPlant * 0.022f * visualSpeedBlend),
                    visualBaseScale.z * (1f + visualSpeedBlend * 0.032f)
                );
                break;

            case EnemyState.TelegraphingAttack:
                float tense = Mathf.Sin(Time.time * 42f) * 0.03f * attackPoseBlend;
                targetPos += Vector3.up * (0.18f + 0.24f * attackPoseBlend) + new Vector3(tense, 0f, tense);
                targetRot *= Quaternion.Euler(-26f * attackPoseBlend, 0f, tense * 20f);
                targetScale = new Vector3(visualBaseScale.x * (1f + .05f * attackPoseBlend), visualBaseScale.y * (1f + .08f * attackPoseBlend), visualBaseScale.z * (1f - .05f * attackPoseBlend));
                break;

            case EnemyState.Slamming:
                targetPos += Vector3.down * .08f;
                targetRot *= Quaternion.Euler(42f * attackPoseBlend, 0f, 0f);
                targetScale = new Vector3(visualBaseScale.x * 1.08f, visualBaseScale.y * 0.88f, visualBaseScale.z * 1.12f);
                break;

            case EnemyState.StuckInGround:
                // Struggling to yank the weapon out (Understory mudLodge)
                float struggle = Mathf.Sin(Time.time * 36f) * (isBoss ? 0.08f : 0.05f);
                targetPos += new Vector3(struggle, 0f, Mathf.Cos(Time.time * 28f) * struggle * 0.5f) * stuckPoseBlend;
                targetRot *= Quaternion.Euler((38f + Mathf.Sin(Time.time * 16f) * 4f) * stuckPoseBlend, 0f, struggle * 32f * stuckPoseBlend);
                break;

            case EnemyState.Recovering:
                targetPos += (Vector3.up * 0.15f - transform.forward * 0.2f) * stuckPoseBlend;
                targetRot *= Quaternion.Euler(-14f * stuckPoseBlend, 0f, 0f);
                break;

            case EnemyState.Stunned:
                float dizzy = Time.time * 4.5f;
                targetPos.y += Mathf.Sin(dizzy) * 0.035f * stunnedPoseBlend;
                targetRot *= Quaternion.Euler(Mathf.Sin(dizzy) * 14f * stunnedPoseBlend, Mathf.Cos(dizzy * 0.5f) * 18f * stunnedPoseBlend, Mathf.Sin(dizzy * 1.4f) * 10f * stunnedPoseBlend);
                break;
        }

        float damping = CurrentState == EnemyState.Chasing ? .085f : .055f;
        visualModel.localPosition = Vector3.SmoothDamp(visualModel.localPosition, targetPos, ref visualPoseVelocity, damping, 12f, dt);
        visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, targetRot, 1f - Mathf.Exp(-20f * dt));
        visualModel.localScale = Vector3.SmoothDamp(visualModel.localScale, targetScale, ref visualScaleVelocity, .07f, 10f, dt);
    }

    private void FixedUpdate()
    {
        if (health <= 0f || CurrentState == EnemyState.Dead) return;
        if (IsRooted) { if (rb != null) SmoothVelocity(Vector3.zero); return; }

        if (Time.time < stunnedUntil)
        {
            CurrentState = EnemyState.Stunned;
            return;
        }

        if (target == null || (campaign != null && !campaign.IsBattleActive))
        {
            if (rb != null) SmoothVelocity(Vector3.zero);
            CurrentState = EnemyState.Idle;
            return;
        }

        // Handle active state machines
        switch (CurrentState)
        {
            case EnemyState.TelegraphingAttack:
                UpdateTelegraphing();
                return;

            case EnemyState.Slamming:
                return;

            case EnemyState.StuckInGround:
                UpdateStuckInGround();
                return;

            case EnemyState.Recovering:
                UpdateRecovering();
                return;

            default:
                CurrentState = EnemyState.Chasing;
                UpdateChasingMovement();
                break;
        }
    }

    private void UpdateChasingMovement()
    {
        if (rb == null) return;

        Vector3 focusPosition = focusTarget != null ? focusTarget.position : target.position;
        Vector3 attackDelta = focusPosition - rb.position;
        attackDelta.y = 0f;
        float attackSqrDist = attackDelta.sqrMagnitude;

        Vector3 desiredSlot = GetReachableFormationPoint(focusPosition);
        Vector3 delta = desiredSlot - rb.position;
        delta.y = 0f;
        float sqrDist = delta.sqrMagnitude;

        // Boss Heavy Slam trigger check
        if (isBoss && Time.time >= nextHeavyAttackTime && attackSqrDist <= heavyAttackRange * heavyAttackRange)
        {
            StartCoroutine(PerformHeavySlamTelegraph());
            return;
        }

        // Minion quick spear thrust check
        if (!isBoss && Time.time >= nextMinionAttackTime && attackSqrDist <= 2.8f * 2.8f)
        {
            StartCoroutine(PerformMinionThrust());
            return;
        }

        // Stopping distance in melee contact
        if (sqrDist < formationTolerance * formationTolerance)
        {
            SmoothVelocity(Vector3.zero);
            Vector3 faceDir = attackDelta.normalized;
            if (faceDir.sqrMagnitude > 0.001f)
            {
                Quaternion faceRot = Quaternion.LookRotation(faceDir, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, faceRot, 8f * Time.fixedDeltaTime));
            }
            return;
        }

        Vector3 direction = delta.normalized;
        direction = ApplyEnemySeparation(direction);

        // Physical obstacle & companion avoidance ray/spherecast
        Vector3 rayOrigin = rb.position + Vector3.up * 0.7f;
        if (Physics.SphereCast(rayOrigin, 0.45f, direction, out RaycastHit hit, 1.6f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target))
            {
                Vector3 normal = hit.normal;
                normal.y = 0f;
                if (normal.sqrMagnitude > 0.01f)
                {
                    Vector3 tangent = Vector3.Cross(Vector3.up, normal.normalized).normalized;
                    if (Vector3.Dot(tangent, direction) < 0f) tangent = -tangent;
                    direction = (direction + tangent * 1.5f).normalized;
                }
            }
        }

        // Apply physical velocity
        float catchUpMultiplier = attackSqrDist > engageDistance * engageDistance ? 1.2f : 1f;
        Vector3 targetVelocity = direction * moveSpeed * catchUpMultiplier;
        SmoothVelocity(targetVelocity);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 8f * Time.fixedDeltaTime));
        }
    }

    private void ResolveIronHorseTarget()
    {
        MountedHorseController mounted = FindAnyObjectByType<MountedHorseController>();
        if (mounted != null)
        {
            target = mounted.transform;
            // Use a dedicated gameplay target on the mounted character root.
            // Never target environment meshes or a visual pivot that may be
            // animated into a wall during rearing/attacks.
            Transform combatFocus = mounted.transform.Find("CombatFocus_IronHorse");
            focusTarget = combatFocus != null ? combatFocus : mounted.transform;
            ComputeFormationOffset();
            return;
        }

        AdventurerController adventurer = FindAnyObjectByType<AdventurerController>();
        if (adventurer != null)
        {
            target = adventurer.transform;
            focusTarget = target;
            ComputeFormationOffset();
        }
    }

    private Vector3 GetReachableFormationPoint(Vector3 focusPosition)
    {
        Vector3 candidate = focusPosition + formationOffset;
        int count = Physics.OverlapSphereNonAlloc(candidate, 0.4f, separationHits, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider hit = separationHits[i];
            if (hit == null || hit == col) continue;
            if (target != null && hit.transform.IsChildOf(target)) continue;
            if (hit.GetComponentInParent<ThanhGiongEnemy>() != null) continue;

            // Static colliders here are architecture/floor props. If a ring slot
            // falls inside one, chase the mounted character itself instead.
            if (hit.attachedRigidbody == null) return focusPosition;
        }
        return candidate;
    }

    private void ComputeFormationOffset()
    {
        // Six soldiers per ring leaves a readable combat lane around the horse.
        // Wider outer rings stop a whole wave from hiding the mounted hero at
        // scene start while still letting the army close in organically.
        int slot = formationSlot >= 0 ? formationSlot : Mathf.Abs(GetEntityId().GetHashCode());
        const int soldiersPerRing = 6;
        int ringIndex = (slot / soldiersPerRing) % 5;
        int positionInRing = slot % soldiersPerRing;
        float angleDegrees = positionInRing * (360f / soldiersPerRing) + ringIndex * 30f;
        float angle = angleDegrees * Mathf.Deg2Rad;
        float ring = surroundRadius + ringIndex * 1.35f;
        if (isBoss) ring += 1.2f;
        formationOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
    }

    private Vector3 ApplyEnemySeparation(Vector3 desiredDirection)
    {
        int count = Physics.OverlapSphereNonAlloc(rb.position, separationRadius, separationHits, ~0, QueryTriggerInteraction.Ignore);
        Vector3 separation = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Collider otherCollider = separationHits[i];
            if (otherCollider == null || otherCollider == col) continue;
            ThanhGiongEnemy other = otherCollider.GetComponentInParent<ThanhGiongEnemy>();
            if (other == null || other == this || other.CurrentState == EnemyState.Dead) continue;

            Vector3 away = rb.position - other.transform.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance > 0.001f)
                separation += away / distance * (1f - Mathf.Clamp01(distance / separationRadius));
        }

        Vector3 combined = desiredDirection + separation * separationStrength;
        return combined.sqrMagnitude > 0.001f ? combined.normalized : desiredDirection;
    }

    private void SmoothVelocity(Vector3 targetVelocity)
    {
        smoothedPlanarVelocity = Vector3.SmoothDamp(smoothedPlanarVelocity, targetVelocity, ref velocityDamp, locomotionSmoothTime, moveSpeed * 4f, Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(smoothedPlanarVelocity.x, rb.linearVelocity.y, smoothedPlanarVelocity.z);
    }

    #region Understory Vulnerability & Heavy Slam Coroutines

    private IEnumerator PerformHeavySlamTelegraph()
    {
        CurrentState = EnemyState.TelegraphingAttack;
        rb.linearVelocity = Vector3.zero;

        // Predict slam position right in front of target or player's current location
        slamTargetPos = target.position;
        Vector3 faceDir = (slamTargetPos - transform.position);
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(faceDir.normalized, Vector3.up);

        // Spawn glowing red telegraph warning ring on ground
        SpawnWarningRing(slamTargetPos, slamRadius);

        // Procedural wind-up: Lean body backward and raise upward
        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            if (health <= 0f || Time.time < stunnedUntil || campaign == null || !campaign.IsBattleActive)
            {
                DestroyWarningRing();
                ResetVisualPose();
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / telegraphDuration;

            if (visualModel != null && visualModel != transform)
            {
                visualModel.localPosition = visualBaseLocalPos + Vector3.up * (Mathf.Sin(t * Mathf.PI * 0.5f) * 0.45f);
                visualModel.localRotation = visualBaseLocalRot * Quaternion.Euler(-25f * t, 0f, 0f);
            }

            yield return null;
        }

        // Phase: Slamming down
        CurrentState = EnemyState.Slamming;
        DestroyWarningRing();

        // Smash down violently forward
        if (visualModel != null && visualModel != transform)
        {
            visualModel.localPosition = visualBaseLocalPos;
            visualModel.localRotation = visualBaseLocalRot * Quaternion.Euler(42f, 0f, 0f);
        }

        // Ground shockwave effects & camera shake
        SpawnGroundImpact(slamTargetPos, slamRadius);
        IsometricCameraFollow.Instance?.Shake(0.7f, 0.45f);

        // Check if player is caught in slam
        Vector3 slamOffset = target.position - slamTargetPos;
        slamOffset.y = 0;
        float distanceToPlayer = slamOffset.magnitude;
        bool hitPlayer = distanceToPlayer <= slamRadius;

        if (hitPlayer)
        {
            campaign?.DamagePlayer(slamDamage, transform.position);
            // Player hit! Boss knocks player back and swiftly recovers
            Vector3 pushDir = (target.position - slamTargetPos).normalized;
            if (pushDir.sqrMagnitude < 0.01f) pushDir = transform.forward;

            Rigidbody targetBody = target.GetComponent<Rigidbody>();
            if (targetBody != null && !targetBody.isKinematic)
            {
                targetBody.AddForce(pushDir * 18f + Vector3.up * 4f, ForceMode.Impulse);
            }


            yield return new WaitForSeconds(0.4f);
            ResetVisualPose();
            nextHeavyAttackTime = Time.time + heavyAttackCooldown;
            CurrentState = EnemyState.Chasing;
        }
        else
        {
            // PLAYER DODGED! WEAPON GETS STUCK IN THE GROUND (Understory mudLodge)!
            CurrentState = EnemyState.StuckInGround;
            stateTimer = stuckDuration;
            SpawnStuckBladeMarker(slamTargetPos);

            // Announce critical vulnerability window to player
            campaign?.ShowMessage("★ SƠ HỞ! ĐẠI ĐAO CỦA TƯỚNG GIẶC GĂM XUỐNG ĐẤT — PHẢN CÔNG GÂY X2 SÁT THƯƠNG! ★", stuckDuration);

            // Spawn stuck sparks & dust puff
            SpawnBladeSparks(slamTargetPos + Vector3.up * 0.2f);

            // Wait out stuck duration while struggling
            yield return new WaitForSeconds(stuckDuration);

            if (CurrentState == EnemyState.StuckInGround && health > 0f)
            {
                // Recover from being stuck
                CurrentState = EnemyState.Recovering;
                DestroyStuckBladeMarker();
                campaign?.ShowMessage("TƯỚNG GIẶC ĐÃ NHỔ ĐƯỢC ĐẠI ĐAO LÊN!", 1.2f);
                SpawnBladeSparks(transform.position + Vector3.up * 0.5f);

                yield return new WaitForSeconds(0.45f);
                ResetVisualPose();
                nextHeavyAttackTime = Time.time + heavyAttackCooldown;
                CurrentState = EnemyState.Chasing;
            }
        }
    }

    private IEnumerator PerformMinionThrust()
    {
        nextMinionAttackTime = Time.time + minionAttackInterval;
        CurrentState = EnemyState.TelegraphingAttack;
        if (rb != null) SmoothVelocity(Vector3.zero);
        Vector3 origPos = visualModel != null ? visualModel.localPosition : Vector3.zero;

        // Quick thrust forward
        if (visualModel != null && visualModel != transform)
        {
            visualModel.localPosition = origPos + Vector3.forward * 0.4f;
            visualModel.localRotation = visualBaseLocalRot * Quaternion.Euler(15f, 0f, 0f);
        }

        yield return new WaitForSeconds(0.22f);
        if (health > 0f && Time.time >= stunnedUntil && target != null && campaign != null &&
            campaign.IsBattleActive && Vector3.Distance(target.position, transform.position) < 3f)
            campaign.DamagePlayer(8f, transform.position);
        ResetVisualPose();
        if (health > 0f && Time.time >= stunnedUntil) CurrentState = EnemyState.Chasing;
    }

    private void UpdateTelegraphing()
    {
        if (rb != null) SmoothVelocity(Vector3.zero);
    }

    private void UpdateStuckInGround()
    {
        if (rb != null) SmoothVelocity(Vector3.zero);
    }

    private void UpdateRecovering()
    {
        if (rb != null) SmoothVelocity(Vector3.zero);
    }

    private void ResetVisualPose()
    {
        if (visualModel != null && visualModel != transform)
        {
            visualModel.localPosition = visualBaseLocalPos;
            visualModel.localRotation = visualBaseLocalRot;
            visualModel.localScale = visualBaseScale;
            visualPoseVelocity = Vector3.zero;
            visualScaleVelocity = Vector3.zero;
        }
    }

    private void SpawnStuckBladeMarker(Vector3 point)
    {
        DestroyStuckBladeMarker();
        stuckBladeMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stuckBladeMarker.name = "Đại Đao Găm Đất — Vulnerability 1.5s";
        stuckBladeMarker.transform.position = point + Vector3.up * .72f;
        stuckBladeMarker.transform.rotation = transform.rotation * Quaternion.Euler(58f, 0f, 0f);
        stuckBladeMarker.transform.localScale = new Vector3(.16f, 1.35f, .08f);
        Collider markerCollider = stuckBladeMarker.GetComponent<Collider>();
        if (markerCollider != null) Destroy(markerCollider);
        Renderer markerRenderer = stuckBladeMarker.GetComponent<Renderer>();
        Material markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        markerMaterial.color = new Color(.24f, .27f, .3f);
        if (markerMaterial.HasProperty("_Metallic")) markerMaterial.SetFloat("_Metallic", .85f);
        markerRenderer.material = markerMaterial;
    }

    private void DestroyStuckBladeMarker()
    {
        if (stuckBladeMarker == null) return;
        Renderer renderer = stuckBladeMarker.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null) Destroy(renderer.material);
        Destroy(stuckBladeMarker);
        stuckBladeMarker = null;
    }

    #endregion

    #region Procedural Visuals & FX

    private void SpawnWarningRing(Vector3 center, float radius)
    {
        DestroyWarningRing();

        activeWarningRing = new GameObject("BossWarningRing");
        activeWarningRing.transform.position = center + Vector3.up * 0.08f;

        LineRenderer line = activeWarningRing.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 36;
        line.startWidth = 0.22f;
        line.endWidth = 0.22f;
        line.numCapVertices = 4;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1.0f, 0.15f, 0.05f, 0.95f);
        line.endColor = new Color(1.0f, 0.5f, 0.1f, 0.95f);

        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = (i / (float)line.positionCount) * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private void DestroyWarningRing()
    {
        if (activeWarningRing != null)
        {
            Destroy(activeWarningRing);
            activeWarningRing = null;
        }
    }

    private void SpawnGroundImpact(Vector3 center, float radius)
    {
        GameObject impact = new GameObject("GroundImpactWave");
        impact.transform.position = center + Vector3.up * 0.06f;

        LineRenderer line = impact.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 32;
        line.startWidth = 0.35f;
        line.endWidth = 0.35f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1.0f, 0.7f, 0.2f, 0.9f);
        line.endColor = new Color(0.8f, 0.3f, 0.05f, 0.0f);

        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = (i / (float)line.positionCount) * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        StartCoroutine(FadeOutImpact(impact, line, 0.45f));
    }

    private void SpawnBladeSparks(Vector3 point)
    {
        GameObject sparks = new GameObject("BladeSparks");
        sparks.transform.position = point;

        LineRenderer line = sparks.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 10;
        line.startWidth = 0.08f;
        line.endWidth = 0.01f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1.0f, 0.95f, 0.3f, 1f);
        line.endColor = new Color(1.0f, 0.3f, 0.05f, 0f);

        for (int i = 0; i < line.positionCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.9f;
            offset.y = Mathf.Abs(offset.y);
            line.SetPosition(i, point + offset);
        }

        Destroy(sparks, 0.35f);
    }

    private void SpawnCritSparks(Vector3 point)
    {
        GameObject crit = new GameObject("VulnerabilityCritSparks");
        crit.transform.position = point;

        LineRenderer line = crit.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 16;
        line.startWidth = 0.15f;
        line.endWidth = 0.02f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1.0f, 0.85f, 0.15f, 1.0f);
        line.endColor = new Color(1.0f, 0.25f, 0.05f, 0.0f);

        for (int i = 0; i < line.positionCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 1.6f;
            offset.y = Mathf.Abs(offset.y) + 0.3f;
            line.SetPosition(i, point + offset);
        }

        Destroy(crit, 0.4f);
    }

    private IEnumerator FadeOutImpact(GameObject obj, LineRenderer line, float duration)
    {
        float elapsed = 0f;
        Color sCol = line.startColor;
        Color eCol = line.endColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            line.startColor = new Color(sCol.r, sCol.g, sCol.b, Mathf.Lerp(sCol.a, 0f, t));
            line.endColor = new Color(eCol.r, eCol.g, eCol.b, Mathf.Lerp(eCol.a, 0f, t));
            yield return null;
        }

        Destroy(obj);
    }

    #endregion

    public void ApplyRoot(float seconds)
    {
        if (seconds <= 0 || health <= 0 || !gameObject.activeInHierarchy || Time.timeScale <= 0) return;
        rootedUntil = Mathf.Max(rootedUntil, Time.time + seconds);
        smoothedPlanarVelocity = Vector3.zero; velocityDamp = Vector3.zero;
        visualSpeedBlend = 0f; visualSpeedBlendVel = 0f;
        if (rb != null && !rb.isKinematic) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
    }

    public void RestoreCheckpoint(ThanhGiongCampaignController owner, Transform player, Vector3 position, Quaternion rotation, float savedHealth, bool alive)
    {
        ResetForBattle(owner, player);
        transform.SetPositionAndRotation(position, rotation);
        if(rb!=null){rb.position=position;rb.rotation=rotation;}
        health = Mathf.Clamp(savedHealth, 0, maxHealth);
        stunnedUntil = 0f;
        rootedUntil = 0f;
        flinchTimer = 0f;
        CurrentState = alive && health > 0 ? EnemyState.Chasing : EnemyState.Dead;
        gameObject.SetActive(alive && health > 0);
    }

    public void TakeDamage(float amount, float stunSeconds = 0f, Vector3 hitSource = default)
    {
        if (!gameObject.activeSelf || health <= 0f) return;

        float actualDamage = amount;

        // VULNERABILITY WINDOW: Multiply damage by 2.0x if struck while weapon is stuck in ground!
        if (CurrentState == EnemyState.StuckInGround)
        {
            actualDamage *= vulnerabilityMultiplier;
            SpawnCritSparks(transform.position + Vector3.up * 1.5f);
            IsometricCameraFollow.Instance?.Shake(0.55f, 0.4f);
            campaign?.ShowMessage($"★ CHÍ MẠNG SƠ HỞ! -{Mathf.RoundToInt(actualDamage)} MÁU (x2 SÁT THƯƠNG) ★", 1.2f);
        }

        health -= actualDamage;
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + stunSeconds);
        flinchTimer = Mathf.Max(flinchTimer, isBoss ? 0.18f : 0.22f);
        transform.localScale = (isBoss ? initialScale * 1.8f : initialScale) * 1.05f;

        Vector3 source = hitSource != default ? hitSource : (target != null ? target.position : transform.position - transform.forward);

        if (health > 0f)
        {
            if (ragdoll != null)
            {
                // Less knockback while stuck so boss doesn't slide away from his stuck weapon
                float knockPower = CurrentState == EnemyState.StuckInGround ? 2.5f : (isBoss ? 6.5f : 13f);
                ragdoll.ApplyKnockback(source, transform.position - source, knockPower, 1.4f);
            }
            return;
        }

        // Defeated!
        CurrentState = EnemyState.Dead;
        DestroyWarningRing();
        DestroyStuckBladeMarker();
        ResetVisualPose();
        campaign?.NotifyEnemyDefeated(this);

        if (ragdoll != null)
        {
            ragdoll.LaunchOnDeath(source, isBoss ? 14f : 24f, isBoss ? 3.0f : 5.0f);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
