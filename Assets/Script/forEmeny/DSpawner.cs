using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

/// <summary>
/// Pooled spawner for EnemySplineFollower.
///
/// Each DSpawner is assigned exactly one NavMeshSurface. Every enemy spawned
/// by this spawner is configured to use that surface's Agent Type.
///
/// Spawn position is no longer derived from a spline. Each spawn picks a
/// random point inside a square area (spawnAreaHalfSize on each axis)
/// centered on this spawner's own transform position.
///
/// IMPORTANT:
/// "Include Layers" controls which scene objects are baked into a surface.
/// It does not uniquely identify that surface at runtime. To keep two surfaces
/// separate, give them different Agent Types.
/// </summary>
public class DSpawner : MonoBehaviour
{
    [Header("Prefab and Pool")]
    [Tooltip("Prefab must contain EnemySplineFollower, Rigidbody, and NavMeshAgent. Keep the prefab's NavMeshAgent disabled.")]
    [SerializeField] private EnemySplineFollower enemyPrefab;

    [SerializeField, Min(1)] private int poolDefaultCapacity = 10;
    [SerializeField, Min(1)] private int poolMaxSize = 50;
    [SerializeField] private bool collectionChecks = true;

    [SerializeField]
    private EnemySplineFollower.LoopMode loopMode =
        EnemySplineFollower.LoopMode.Once;

    [Header("Spawn Area")]
    [Tooltip("Half-size of a square spawn area (world units), centered on this spawner's transform. Each spawn picks a random X/Z offset within [-half, half] on both axes — a square footprint, not a circular radius.")]
    [SerializeField, Min(0f)] private float spawnAreaHalfSize = 3f;

    [Tooltip("Color of the spawn area gizmo drawn in the Scene view.")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.4f, 0f, 1f);

    [Tooltip("If true, the spawn area gizmo is always drawn. If false, it's only drawn while this object is selected.")]
    [SerializeField] private bool alwaysDrawGizmo = false;

    [Header("Assigned NavMesh Surface")]
    [Tooltip("Enemies spawned here use this surface's Agent Type.")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Tooltip("How far from the spawn point the spawner checks for this surface's NavMesh.")]
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 5f;

    [Tooltip("Require the surface to have baked NavMesh data before spawning.")]
    [SerializeField] private bool requireBakedSurface = true;

    [Header("Spawning")]
    [SerializeField, Min(0.02f)] private float spawnInterval = 1.5f;
    [SerializeField] private bool spawnOneAtATime = true;
    [SerializeField, Min(1)] private int maxAliveEnemies = 20;
    [SerializeField] private bool autoStart = true;

    private ObjectPool<EnemySplineFollower> pool;

    private readonly HashSet<EnemySplineFollower> activeEnemies =
        new HashSet<EnemySplineFollower>();

    private Coroutine spawnRoutine;

    public int ActiveCount => activeEnemies.Count;
    public NavMeshSurface AssignedSurface => navMeshSurface;

    private void Awake()
    {
        pool = new ObjectPool<EnemySplineFollower>(
            createFunc: CreateEnemy,
            actionOnGet: OnGetEnemy,
            actionOnRelease: OnReleaseEnemy,
            actionOnDestroy: OnDestroyEnemy,
            collectionCheck: collectionChecks,
            defaultCapacity: poolDefaultCapacity,
            maxSize: poolMaxSize);
    }

    private void Start()
    {
        ValidateSpawnerSetup();

        if (autoStart)
            StartSpawning();
    }

    private void OnDestroy()
    {
        StopSpawning();
        pool?.Clear();
    }

    private void OnValidate()
    {
        poolDefaultCapacity = Mathf.Max(1, poolDefaultCapacity);
        poolMaxSize = Mathf.Max(poolDefaultCapacity, poolMaxSize);
        navMeshSampleRadius = Mathf.Max(0.1f, navMeshSampleRadius);
        spawnInterval = Mathf.Max(0.02f, spawnInterval);
        maxAliveEnemies = Mathf.Max(1, maxAliveEnemies);
        spawnAreaHalfSize = Mathf.Max(0f, spawnAreaHalfSize);
    }

    public void StartSpawning()
    {
        if (spawnRoutine != null)
            return;

        if (!CanSpawn())
            return;

        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnRoutine == null)
            return;

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    public void SetNavMeshSurface(NavMeshSurface surface)
    {
        navMeshSurface = surface;
        ValidateSpawnerSetup();
    }

    public EnemySplineFollower SpawnEnemy()
    {
        if (!CanSpawn())
            return null;

        EnemySplineFollower enemy = pool.Get();

        // Configure the inactive pooled object before enabling it.
        if (!ConfigureAgentForAssignedSurface(enemy))
        {
            ReleaseToPool(enemy);
            return null;
        }

        Vector3 spawnPosition = GetJitteredSpawnPosition();

        // Activate BEFORE Initialize(). NavMeshAgent.Warp() (called inside
        // Initialize -> BeginRoamingFromSpawn) requires the GameObject to already
        // be active in the hierarchy — a NavMeshAgent on an inactive object hasn't
        // run its own OnEnable() yet and isn't registered with the nav system, so
        // Warp() silently fails to place it on the mesh. That left the agent
        // permanently off-mesh (isOnNavMesh == false), which is why enemies were
        // spawning but never moving. Position/health are still set synchronously
        // inside Initialize() right after this, so there's no visible pop-in.
        enemy.gameObject.SetActive(true);

        // Initialize() places the enemy at spawnPosition and immediately starts
        // it wandering/chasing on the NavMesh using the Agent Type just assigned above.
        // NOTE: spline params are passed as null/0 since positioning no longer
        // depends on a spline. Confirm EnemySplineFollower.Initialize tolerates
        // a null container if it still references it internally.
        enemy.Initialize(
            null,
            0,
            loopMode,
            spawnPositionOverride: spawnPosition);

        ValidateSpawnNearAssignedSurface(enemy);

        return enemy;
    }

    /// <summary>
    /// Picks a random point within a square area on the X/Z plane,
    /// centered on this spawner's transform position.
    /// </summary>
    private Vector3 GetJitteredSpawnPosition()
    {
        Vector3 basePosition = transform.position;

        if (spawnAreaHalfSize <= 0f)
            return basePosition;

        float offsetX = Random.Range(-spawnAreaHalfSize, spawnAreaHalfSize);
        float offsetZ = Random.Range(-spawnAreaHalfSize, spawnAreaHalfSize);

        return basePosition + new Vector3(offsetX, 0f, offsetZ);
    }

    private bool CanSpawn()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning(
                "[DSpawner] No EnemySplineFollower prefab is assigned.",
                this);
            return false;
        }

        if (navMeshSurface == null)
        {
            Debug.LogWarning(
                "[DSpawner] No NavMeshSurface is assigned.",
                this);
            return false;
        }

        if (requireBakedSurface &&
            navMeshSurface.navMeshData == null)
        {
            Debug.LogWarning(
                $"[DSpawner] NavMeshSurface '{navMeshSurface.name}' has no baked NavMesh data.",
                navMeshSurface);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Assigns this spawner's NavMeshSurface Agent Type onto the enemy before it
    /// initializes. We set EnemySplineFollower.GroundAgentTypeID (not the
    /// NavMeshAgent component directly) because Initialize() re-applies that value
    /// and re-warps the agent onto it — setting the component field directly here
    /// would just get overwritten a moment later.
    /// </summary>
    private bool ConfigureAgentForAssignedSurface(
        EnemySplineFollower enemy)
    {
        if (enemy == null || navMeshSurface == null)
            return false;

        if (!enemy.TryGetComponent(out NavMeshAgent agent))
        {
            Debug.LogError(
                "[DSpawner] Spawned enemy has no NavMeshAgent component.",
                enemy);
            return false;
        }

        // Must be disabled before Initialize() re-enables and warps it.
        agent.enabled = false;

        enemy.GroundAgentTypeID = navMeshSurface.agentTypeID;

        return true;
    }

    /// <summary>
    /// Checks for a valid point matching the assigned Agent Type near the enemy's
    /// spawn position. This is a diagnostic warning only — EnemySplineFollower
    /// already warps and warns internally if placement fails; this just gives an
    /// earlier, spawner-context heads-up.
    /// </summary>
    private void ValidateSpawnNearAssignedSurface(
        EnemySplineFollower enemy)
    {
        if (enemy == null || navMeshSurface == null)
            return;

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = navMeshSurface.agentTypeID,
            areaMask = NavMesh.AllAreas
        };

        if (!NavMesh.SamplePosition(
                enemy.transform.position,
                out _,
                navMeshSampleRadius,
                filter))
        {
            Debug.LogWarning(
                $"[DSpawner] '{enemy.name}' starts more than " +
                $"{navMeshSampleRadius} units from NavMeshSurface " +
                $"'{navMeshSurface.name}'. Agent Type ID: " +
                $"{navMeshSurface.agentTypeID}. " +
                "The enemy may fail to be placed on the NavMesh at spawn.",
                enemy);
        }
    }

    private void ValidateSpawnerSetup()
    {
        if (navMeshSurface == null)
            return;

        Debug.Log(
            $"[DSpawner] '{name}' uses NavMeshSurface " +
            $"'{navMeshSurface.name}', Agent Type ID " +
            $"{navMeshSurface.agentTypeID}.",
            this);
    }

    private IEnumerator SpawnLoop()
    {
        WaitForSeconds wait =
            new WaitForSeconds(spawnInterval);

        while (true)
        {
            if (spawnOneAtATime)
            {
                if (activeEnemies.Count == 0)
                    SpawnEnemy();
            }
            else if (activeEnemies.Count < maxAliveEnemies)
            {
                SpawnEnemy();
            }

            yield return wait;
        }
    }

    // ---------------------------------------------------------------------
    // Pool callbacks
    // ---------------------------------------------------------------------

    private EnemySplineFollower CreateEnemy()
    {
        EnemySplineFollower enemy =
            Instantiate(enemyPrefab, transform);

        // The prefab's NavMeshAgent should already be disabled. This is an
        // additional safety step after instantiation.
        if (enemy.TryGetComponent(out NavMeshAgent agent))
            agent.enabled = false;

        enemy.gameObject.SetActive(false);
        enemy.OnDeath += HandleEnemyDied;

        return enemy;
    }

    private void OnGetEnemy(EnemySplineFollower enemy)
    {
        activeEnemies.Add(enemy);

        // Keep inactive until SpawnEnemy finishes configuration.
        enemy.gameObject.SetActive(false);
    }

    private void OnReleaseEnemy(EnemySplineFollower enemy)
    {
        if (enemy == null)
            return;

        activeEnemies.Remove(enemy);
        enemy.PrepareForPool();

        if (enemy.gameObject.activeSelf)
            enemy.gameObject.SetActive(false);
    }

    private void OnDestroyEnemy(EnemySplineFollower enemy)
    {
        if (enemy == null)
            return;

        enemy.OnDeath -= HandleEnemyDied;
        Destroy(enemy.gameObject);
    }

    /// <summary>
    /// EnemySplineFollower no longer has an "end of spline" event to release on —
    /// enemies now wander/chase indefinitely, so the pool release point is death.
    /// Enemies flagged DoNotRespawnOnDeath (bosses/uniques) are destroyed outright
    /// instead of being recycled.
    /// </summary>
    private void HandleEnemyDied(EnemySplineFollower enemy)
    {
        if (enemy == null)
            return;

        if (enemy.DoNotRespawnOnDeath)
        {
            activeEnemies.Remove(enemy);
            enemy.OnDeath -= HandleEnemyDied;
            Destroy(enemy.gameObject);
            return;
        }

        ReleaseToPool(enemy);
    }

    public void ReleaseToPool(
        EnemySplineFollower enemy)
    {
        if (enemy == null ||
            !activeEnemies.Contains(enemy))
        {
            return;
        }

        pool.Release(enemy);
    }

    // ---------------------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (alwaysDrawGizmo)
            DrawSpawnAreaGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!alwaysDrawGizmo)
            DrawSpawnAreaGizmo();
    }

    private void DrawSpawnAreaGizmo()
    {
        if (spawnAreaHalfSize <= 0f)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, 0.2f);
            return;
        }

        Gizmos.color = gizmoColor;
        Vector3 size = new Vector3(spawnAreaHalfSize * 2f, 0.05f, spawnAreaHalfSize * 2f);
        Gizmos.DrawWireCube(transform.position, size);

        // Faint filled square so the area reads clearly even at a glance.
        Color fill = gizmoColor;
        fill.a = 0.08f;
        Gizmos.color = fill;
        Gizmos.DrawCube(transform.position, size);
    }
}