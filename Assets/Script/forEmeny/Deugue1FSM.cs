using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine.Splines;

[RequireComponent(typeof(Rigidbody))]
public class EnemySplineFollower : MonoBehaviour
{
    // Kept only so existing spawner code that calls Initialize(container, index, mode, ...)
    // doesn't break. Movement is no longer spline-based (see Wander settings below);
    // this is now used only to pick the enemy's spawn position, if assigned.
    public enum LoopMode { Once, Loop, PingPong }

    [Header("Spawn (optional — used only to place the enemy at Start)")]
    public SplineContainer splineContainer;
    public int splineIndex = 0;
    public LoopMode loopMode = LoopMode.Once; // unused for movement now, kept for compatibility

    [Header("Movement")]
    [Tooltip("World units per second. Mirrors this onto the NavMeshAgent's speed.")]
    [SerializeField] private float moveSpeed = 3f;
    public bool alignToDirection = true;
    public float rotationSpeed = 10f;

    [Header("Health")]
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int currentHP;

    [Header("Pooling")]
    [Tooltip("If true, this instance is permanently removed (destroyed) on death instead of being returned to the spawner's pool for reuse. Use for uniques/bosses.")]
    [SerializeField] private bool doNotRespawnOnDeath = false;
    public bool DoNotRespawnOnDeath => doNotRespawnOnDeath;

    [Header("Wander")]
    [Tooltip("How far from wherever the enemy currently is that it will pick a new random destination while roaming.")]
    [SerializeField] private float wanderRadius = 10f;
    [Tooltip("How close (world units) counts as 'arrived' at a wander point.")]
    [SerializeField] private float wanderArrivalDistance = 0.3f;
    [Tooltip("Minimum seconds to idle at a wander point before picking a new one.")]
    [SerializeField] private float minWanderWaitTime = 0.5f;
    [Tooltip("Maximum seconds to idle at a wander point before picking a new one.")]
    [SerializeField] private float maxWanderWaitTime = 2f;

    [Header("Player Detection / Chase")]
    [Tooltip("Optional manual assignment. If left empty, the enemy will try to find an object tagged with Player Tag at startup.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Tag used to auto-find the player if Player Transform isn't assigned.")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("How often (seconds) the enemy checks the distance to the player while roaming.")]
    [SerializeField] private float playerCheckInterval = 0.2f;
    [Tooltip("Distance within which the enemy will start chasing the player.")]
    [SerializeField] private float playerDetectionRadius = 8f;
    [Tooltip("Distance beyond which the enemy gives up the chase and returns to roaming. Should be >= Player Detection Radius to avoid rapid state flip-flopping.")]
    [SerializeField] private float chaseLoseRadius = 12f;
    [Tooltip("How often (seconds) the destination is refreshed to the player's current position while chasing.")]
    [SerializeField] private float chaseDestinationUpdateInterval = 0.15f;
    [Tooltip("Distance at which this enemy counts as having 'reached' the player, triggering an infection increase.")]
    [SerializeField, Min(0f)] private float reachPlayerDistance = 1.5f;
    [Tooltip("Minimum seconds between infection increases from this enemy while it stays in contact with the player, so standing on the player doesn't spam the meter every physics tick.")]
    [SerializeField, Min(0.05f)] private float reachPlayerCooldown = 1f;

    [Header("NavMesh")]
    [Tooltip("The single NavMeshAgent used for wandering and chasing. If left empty, GetComponent<NavMeshAgent>() is used automatically at Awake.")]
    [SerializeField] private NavMeshAgent navAgent;
    [Tooltip("AgentTypeID of the NavMeshSurface baked for ground movement.")]
    [SerializeField] private int groundAgentTypeID = 0;
    [Tooltip("Extra buffer (world units) added on top of the relevant detection radius when searching for a valid NavMesh point to Warp() onto or path toward.")]
    [SerializeField] private float warpSearchBuffer = 1f;

    [Header("Star Rating")]
    [Tooltip("Optional manual assignment. If left empty, ValuesForStar.Instance is used at Awake so this doesn't have to be wired up per-prefab.")]
    [SerializeField] private ValuesForStar valuesForStar;

    public event System.Action<EnemySplineFollower> OnDeath;
    public event System.Action<int, int> OnHealthChanged; // (current, max)

    // Guards against Start() re-doing the work Initialize() already did
    // the first time a pooled instance is activated (see CreateEnemy in
    // EnemySpawner, which activates the object right after Initialize()).
    private bool hasBegunRoam = false;

    // Guards against ReportEnemyKilled() firing more than once for the same
    // death, in case DeathState.Enter is ever re-entered (e.g. a future
    // pooling change that re-runs Enter without a fresh Initialize()).
    // Reset in Initialize() whenever this instance is (re)handed out.
    private bool hasReportedDeath = false;

    // Cooldown gate for infection contact — see NotifyReachedPlayer().
    private float lastReachPlayerTime = -999f;

    private Rigidbody rb;

    public float MoveSpeed
    {
        get => moveSpeed;
        set
        {
            moveSpeed = Mathf.Max(0f, value);
            if (navAgent != null) navAgent.speed = moveSpeed;
        }
    }

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsDead => currentHP <= 0;

    /// <summary>
    /// The NavMesh Agent Type this enemy wanders/chases on. Exposed so a spawner tied to a
    /// specific NavMeshSurface can assign it before Initialize() runs (e.g. one prefab reused
    /// across zones baked with different Agent Types). Setting this after the enemy has already
    /// started roaming has no effect until the next Initialize() call.
    /// </summary>
    public int GroundAgentTypeID
    {
        get => groundAgentTypeID;
        set => groundAgentTypeID = value;
    }

    // ── State machine ──
    private IEnemyState currentState;

    private void Awake()
    {
        currentHP = maxHP;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        // Fall back to the singleton if the field wasn't wired up in the Inspector,
        // same pattern as playerTransform above.
        if (valuesForStar == null)
            valuesForStar = ValuesForStar.Instance;

        // Fall back to GetComponent if the field wasn't wired up in the Inspector.
        // Previously, a missing reference here caused every downstream check
        // (Awake setup, BeginRoamingFromSpawn, RoamState/ChaseState) to silently
        // no-op — the enemy would sit motionless with no console output at all.
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        if (navAgent == null)
        {
            Debug.LogError(
                $"[EnemySplineFollower] '{name}' has no NavMeshAgent component and none is assigned. " +
                "This enemy will never move. Add a NavMeshAgent to the prefab.",
                this);
        }
        else
        {
            navAgent.speed = moveSpeed;
            navAgent.updateRotation = alignToDirection;
            navAgent.enabled = false; // enabled once we know where to warp it, in Start/Initialize
        }
    }

    private void Start()
    {
        if (hasBegunRoam) return;
        BeginRoamingFromSpawn();
    }

    private void FixedUpdate()
    {
        currentState?.Tick(this);
    }

    // Fires whenever this GameObject is deactivated for any reason — eaten
    // (SetActive(false) from MeleeAttack2/SuperEat's EatTarget), returned to
    // the pool, or manually disabled elsewhere. This is the ONLY place the
    // inflammation source gets unregistered now: dying alone (DeathState)
    // leaves the corpse active in the scene, so the inflammation bar should
    // keep counting it as a source until it's actually removed/eaten.
    private void OnDisable()
    {
        if (InflammationManager.Instance != null)
            InflammationManager.Instance.UnregisterSource(this);
    }

    /// <summary>
    /// Re-initializes this enemy for (re)use — the entry point a spawner or
    /// object pool should call every time an instance is handed out, since
    /// Awake()/Start() only ever run once per object lifetime.
    /// </summary>
    /// <param name="spawnPositionOverride">
    /// If provided, the enemy spawns here instead of at the spline's t=0 point
    /// (e.g. a spawner jittering the spawn point within an area).
    /// </param>
    public void Initialize(SplineContainer container, int index, LoopMode mode, int? overrideMaxHP = null, Vector3? spawnPositionOverride = null)
    {
        splineContainer = container;
        splineIndex = index;
        loopMode = mode; // unused for movement, kept for compatibility

        if (overrideMaxHP.HasValue)
            maxHP = Mathf.Max(1, overrideMaxHP.Value);

        currentHP = maxHP;
        hasReportedDeath = false;

        OnHealthChanged?.Invoke(currentHP, maxHP);
        BeginRoamingFromSpawn(spawnPositionOverride);
    }

    private void BeginRoamingFromSpawn(Vector3? spawnPositionOverride = null)
    {
        Vector3 spawnPos = transform.position;

        if (spawnPositionOverride.HasValue)
            spawnPos = spawnPositionOverride.Value;
        else if (splineContainer != null && splineContainer.Splines.Count > splineIndex)
            spawnPos = splineContainer.EvaluatePosition(splineIndex, 0f);

        rb.position = spawnPos;
        transform.position = spawnPos;

        if (navAgent == null)
        {
            Debug.LogError(
                $"[EnemySplineFollower] '{name}' cannot begin roaming — no NavMeshAgent present.",
                this);
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError(
                $"[EnemySplineFollower] '{name}' Initialize() was called while its GameObject " +
                "is inactive. NavMeshAgent.Warp() requires an active hierarchy — call " +
                "gameObject.SetActive(true) BEFORE Initialize(), not after, or the agent will " +
                "silently fail to be placed on the NavMesh and never move.",
                this);
            return;
        }

        navAgent.enabled = true;
        navAgent.agentTypeID = groundAgentTypeID;

        float warpSearchRadius = wanderRadius + warpSearchBuffer;
        bool warped = TryWarpAgentOntoMesh(navAgent, spawnPos, warpSearchRadius, groundAgentTypeID);
        if (!warped)
        {
            Debug.LogWarning($"[EnemySplineFollower] Could not place NavMeshAgent on NavMesh for groundAgentTypeID {groundAgentTypeID} near {spawnPos} at spawn. Check that a NavMeshSurface with that Agent Type is baked here.", this);
            navAgent.enabled = false;
        }

        ChangeState(new RoamState());
        hasBegunRoam = true;

        if (InflammationManager.Instance != null)
            InflammationManager.Instance.RegisterSource(this);
    }

    /// <summary>
    /// Called by a pool right before this instance is stashed away for reuse. Stops the
    /// current state cleanly and disables the NavMeshAgent so it doesn't keep pathing while
    /// inactive. Initialize() fully resets health/position/state when the instance is handed
    /// back out, so this only needs to handle the "going to sleep" side. Unregistering from
    /// InflammationManager happens in OnDisable() once the pool actually deactivates the
    /// GameObject, not here.
    /// </summary>
    public void PrepareForPool()
    {
        currentState?.Exit(this);
        currentState = null;

        if (navAgent != null)
            navAgent.enabled = false;
    }

    private void ChangeState(IEnemyState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState?.Enter(this);
    }

    // ── Health API ───────────────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
            ChangeState(new DeathState());
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    // ── Detection helper ─────────────────────────────────────────────────

    private bool IsPlayerWithinRadius(float radius)
    {
        if (playerTransform == null || radius <= 0f) return false;
        return (playerTransform.position - transform.position).sqrMagnitude <= radius * radius;
    }

    /// <summary>
    /// Called from ChaseState each Tick while this enemy is within
    /// reachPlayerDistance of the player. Gated by reachPlayerCooldown so
    /// sustained contact doesn't ramp the infection meter every FixedUpdate.
    /// </summary>
    private void NotifyReachedPlayer()
    {
        if (Time.time < lastReachPlayerTime + reachPlayerCooldown) return;
        lastReachPlayerTime = Time.time;

        if (InfectionManager.Instance != null)
            InfectionManager.Instance.RegisterEnemyReachedPlayer();
    }

    /// <summary>
    /// Called once from DeathState.Enter when this enemy dies. Reports the kill to
    /// whichever ValuesForStar reference is available — the Inspector-assigned one if
    /// present, otherwise ValuesForStar.Instance as resolved (or re-resolved) here in
    /// case Awake ran before the singleton existed in the scene.
    /// </summary>
    private void ReportDeathToValuesForStar()
    {
        if (valuesForStar == null)
            valuesForStar = ValuesForStar.Instance;

        if (valuesForStar != null)
            valuesForStar.ReportEnemyKilled();
    }

    // ── NavMesh helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Finds a point that is actually valid for the given agentTypeID near worldPosition.
    /// NavMesh.SamplePosition does NOT filter by agent type (its last param is an area
    /// mask, not agent type), so it can return points from the wrong baked mesh entirely.
    /// NavMeshQuery.MapLocation takes agentTypeId directly, so we use that instead for
    /// anything agent-type-sensitive.
    /// </summary>
    private static bool TryFindPointOnMesh(Vector3 worldPosition, float searchExtent, int agentTypeID, out Vector3 result)
    {
        using (var query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Temp))
        {
            NavMeshLocation location = query.MapLocation(worldPosition, Vector3.one * searchExtent, agentTypeID, NavMesh.AllAreas);
            if (query.IsValid(location))
            {
                result = location.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    private static bool TryWarpAgentOntoMesh(NavMeshAgent agent, Vector3 worldPosition, float searchRadius, int agentTypeID)
    {
        if (agent == null) return false;
        if (!TryFindPointOnMesh(worldPosition, searchRadius, agentTypeID, out Vector3 point)) return false;

        bool warped = agent.Warp(point);
        return warped && agent.isOnNavMesh;
    }

    // ── FSM ──────────────────────────────────────────────────────────────

    private interface IEnemyState
    {
        void Enter(EnemySplineFollower enemy);
        void Tick(EnemySplineFollower enemy);
        void Exit(EnemySplineFollower enemy);
    }

    /// <summary>Wanders to random nearby points on the ground NavMesh, while watching for the player.</summary>
    private class RoamState : IEnemyState
    {
        private float playerCheckTimer;
        private float wanderWaitTimer;
        private bool hasDestination;

        public void Enter(EnemySplineFollower enemy)
        {
            playerCheckTimer = enemy.playerCheckInterval;
            hasDestination = false;
            wanderWaitTimer = 0f;

            if (enemy.navAgent != null && enemy.navAgent.enabled && enemy.navAgent.isOnNavMesh)
                PickNewWanderDestination(enemy);
        }

        public void Tick(EnemySplineFollower enemy)
        {
            if (enemy.navAgent == null || !enemy.navAgent.enabled || !enemy.navAgent.isOnNavMesh) return;

            // Player detection — interrupts wandering.
            playerCheckTimer -= Time.fixedDeltaTime;
            if (playerCheckTimer <= 0f)
            {
                playerCheckTimer = enemy.playerCheckInterval;
                if (enemy.IsPlayerWithinRadius(enemy.playerDetectionRadius))
                {
                    enemy.ChangeState(new ChaseState());
                    return;
                }
            }

            // Wander logic.
            if (!hasDestination)
            {
                PickNewWanderDestination(enemy);
                return;
            }

            if (enemy.navAgent.pathPending) return;

            bool arrived = enemy.navAgent.remainingDistance <= enemy.wanderArrivalDistance
                           && (!enemy.navAgent.hasPath || enemy.navAgent.velocity.sqrMagnitude < 0.01f);

            if (arrived)
            {
                wanderWaitTimer -= Time.fixedDeltaTime;
                if (wanderWaitTimer <= 0f)
                    PickNewWanderDestination(enemy);
            }
        }

        public void Exit(EnemySplineFollower enemy) { }

        private void PickNewWanderDestination(EnemySplineFollower enemy)
        {
            Vector2 offset = Random.insideUnitCircle * enemy.wanderRadius;
            Vector3 rawPoint = enemy.transform.position + new Vector3(offset.x, 0f, offset.y);

            float searchExtent = enemy.wanderRadius + enemy.warpSearchBuffer;
            if (TryFindPointOnMesh(rawPoint, searchExtent, enemy.groundAgentTypeID, out Vector3 destination)
                && enemy.navAgent.SetDestination(destination))
            {
                hasDestination = true;
                wanderWaitTimer = Random.Range(enemy.minWanderWaitTime, enemy.maxWanderWaitTime);
            }
            else
            {
                // Couldn't find a valid point this attempt (e.g. edge of mesh) — try again next Tick.
                hasDestination = false;
            }
        }
    }

    /// <summary>Chases the player by repeatedly updating the NavMeshAgent's destination to the player's position.</summary>
    private class ChaseState : IEnemyState
    {
        private float destinationUpdateTimer;

        public void Enter(EnemySplineFollower enemy)
        {
            destinationUpdateTimer = 0f; // force an immediate destination update on first Tick
        }

        public void Tick(EnemySplineFollower enemy)
        {
            if (enemy.navAgent == null || !enemy.navAgent.enabled || !enemy.navAgent.isOnNavMesh || enemy.playerTransform == null)
            {
                enemy.ChangeState(new RoamState());
                return;
            }

            if (!enemy.IsPlayerWithinRadius(enemy.chaseLoseRadius))
            {
                enemy.ChangeState(new RoamState());
                return;
            }

            if (enemy.IsPlayerWithinRadius(enemy.reachPlayerDistance))
                enemy.NotifyReachedPlayer();

            destinationUpdateTimer -= Time.fixedDeltaTime;
            if (destinationUpdateTimer <= 0f)
            {
                destinationUpdateTimer = enemy.chaseDestinationUpdateInterval;
                enemy.navAgent.SetDestination(enemy.playerTransform.position);
            }
        }

        public void Exit(EnemySplineFollower enemy) { }
    }

    /// <summary>
    /// 0 HP. Fires OnDeath, then stops the enemy in place instead of
    /// disabling the GameObject — the corpse stays visible/in the scene,
    /// it just can no longer move (NavMeshAgent disabled, no further
    /// wander/chase logic runs since Tick() here is a no-op). It also
    /// deliberately does NOT unregister from InflammationManager — the
    /// inflammation bar should keep counting this enemy as a source until
    /// it's actually eaten or otherwise deactivated (see OnDisable()).
    /// Also reports this kill to ValuesForStar (WBC / "enemies killed"
    /// tally) exactly once per death, guarded by hasReportedDeath.
    /// </summary>
    private class DeathState : IEnemyState
    {
        public void Enter(EnemySplineFollower enemy)
        {
            if (enemy.navAgent != null)
            {
                if (enemy.navAgent.isOnNavMesh)
                    enemy.navAgent.ResetPath();

                enemy.navAgent.enabled = false;
            }

            if (!enemy.hasReportedDeath)
            {
                enemy.hasReportedDeath = true;
                enemy.ReportDeathToValuesForStar();
            }

            enemy.OnDeath?.Invoke(enemy);
        }

        public void Tick(EnemySplineFollower enemy) { }
        public void Exit(EnemySplineFollower enemy) { }
    }
}