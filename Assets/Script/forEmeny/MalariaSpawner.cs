using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Spawns MalariaFSM enemies at random points inside a box volume, snapped
/// onto the baked NavMesh so they always spawn somewhere walkable.
///
/// Setup:
///   1. Add this component to an empty GameObject.
///   2. Add a BoxCollider to the same GameObject (or assign one elsewhere
///      via spawnArea) and size/position it to cover the region you want
///      enemies to spawn in. It's auto-set to isTrigger so it won't block
///      physics.
///   3. Assign the Malaria enemy prefab (must have MalariaFSM + NavMeshAgent).
///
/// Spawning logic:
///   - Picks a uniformly random point inside the box (in the box's local
///     space, so rotated/scaled boxes work correctly).
///   - Uses NavMesh.SamplePosition to snap that point onto the nearest valid
///     NavMesh surface within navMeshSampleRadius. If no valid NavMesh point
///     is found nearby, it retries with a new random point (up to
///     maxSampleAttempts times) rather than spawning off-mesh.
///   - Respects maxAlive so you don't get unbounded enemy counts, and tracks
///     active enemies by pruning destroyed/deactivated ones each frame.
///
/// Pooling:
///   - If useObjectPool is true, enemies are reused via SetActive rather
///     than Instantiate/Destroy, which pairs with MalariaFSM's OnEnable
///     (it resets target/state whenever the object is reactivated).
///   - If false, every spawn does a fresh Instantiate (simpler, but no reuse).
/// </summary>
public class MalariaSpawner : MonoBehaviour
{
    [Header("Spawn Prefab")]
    [Tooltip("Prefab to spawn. Must have a MalariaFSM + NavMeshAgent on it.")]
    [SerializeField] private GameObject malariaPrefab;

    [Header("Spawn Area")]
    [Tooltip("Box defining the spawn volume. If left empty, uses a BoxCollider on this GameObject.")]
    [SerializeField] private BoxCollider spawnArea;

    [Header("Spawn Settings")]
    [Tooltip("Hard cap on enemies alive at once from this spawner.")]
    [SerializeField] private int maxAlive = 10;

    [Tooltip("How many enemies to spawn immediately on Start.")]
    [SerializeField] private int initialSpawnCount = 5;

    [Tooltip("Seconds between spawn attempts once below maxAlive.")]
    [SerializeField] private float spawnInterval = 3f;

    [Tooltip("Reuse deactivated enemy instances instead of Instantiate/Destroy.")]
    [SerializeField] private bool useObjectPool = true;

    [Tooltip("How many instances to pre-create up front if pooling is enabled.")]
    [SerializeField] private int poolPrewarmCount = 10;

    [Header("NavMesh Sampling")]
    [Tooltip("Max distance a random box point can be from a valid NavMesh surface to still count as a hit.")]
    [SerializeField] private float navMeshSampleRadius = 2f;

    [Tooltip("How many random points to try before giving up on a single spawn.")]
    [SerializeField] private int maxSampleAttempts = 30;

    private readonly List<GameObject> pool = new List<GameObject>();
    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private float spawnTimer;

    private void Awake()
    {
        if (spawnArea == null)
            spawnArea = GetComponent<BoxCollider>();

        if (spawnArea == null)
        {
            Debug.LogError($"[MalariaSpawner] {name}: no BoxCollider assigned or found on this GameObject — the spawner needs a box to spawn inside.");
            enabled = false;
            return;
        }

        // Shouldn't physically block anything, it's just a spawn volume.
        spawnArea.isTrigger = true;

        if (useObjectPool)
            PrewarmPool();
    }

    private void Start()
    {
        for (int i = 0; i < initialSpawnCount; i++)
            SpawnOne();
    }

    private void Update()
    {
        activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);

        if (activeEnemies.Count >= maxAlive)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            SpawnOne();
        }
    }

    // ---------------------------------------------------------------
    // Pooling
    // ---------------------------------------------------------------

    private void PrewarmPool()
    {
        for (int i = 0; i < poolPrewarmCount; i++)
        {
            GameObject obj = Instantiate(malariaPrefab, transform);
            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    private GameObject GetFromPool()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].activeInHierarchy)
                return pool[i];
        }

        // Pool exhausted — grow it rather than failing the spawn.
        GameObject newObj = Instantiate(malariaPrefab, transform);
        newObj.SetActive(false);
        pool.Add(newObj);
        return newObj;
    }

    // ---------------------------------------------------------------
    // Spawning
    // ---------------------------------------------------------------

    public void SpawnOne()
    {
        if (malariaPrefab == null)
        {
            Debug.LogWarning($"[MalariaSpawner] {name}: no prefab assigned, skipping spawn.");
            return;
        }

        if (!TryGetRandomNavMeshPointInBox(out Vector3 spawnPos))
        {
            Debug.LogWarning($"[MalariaSpawner] {name}: couldn't find a valid NavMesh point inside the box after {maxSampleAttempts} attempts. Check that the box overlaps baked NavMesh, or increase navMeshSampleRadius.");
            return;
        }

        GameObject enemy;
        if (useObjectPool)
        {
            enemy = GetFromPool();

            // Activate first, then Warp. Warp (rather than just setting
            // transform.position) explicitly syncs the NavMeshAgent's internal
            // "am I on the mesh" state immediately instead of waiting for it to
            // pick this up on its own — MalariaFSM's OnEnable-triggered reset
            // is deferred exactly until isOnNavMesh reports true, so doing this
            // means a pooled enemy starts roaming the same frame it's spawned
            // instead of sitting a frame (or more) in its stale leftover state.
            enemy.SetActive(true);
            enemy.transform.rotation = Quaternion.identity;

            NavMeshAgent enemyAgent = enemy.GetComponent<NavMeshAgent>();
            if (enemyAgent != null)
            {
                enemyAgent.Warp(spawnPos);
            }
            else
            {
                enemy.transform.position = spawnPos;
            }
        }
        else
        {
            enemy = Instantiate(malariaPrefab, spawnPos, Quaternion.identity, transform);
        }

        activeEnemies.Add(enemy);
    }

    private bool TryGetRandomNavMeshPointInBox(out Vector3 result)
    {
        for (int i = 0; i < maxSampleAttempts; i++)
        {
            // Random point in the box's local space (so rotation/scale on the
            // BoxCollider's transform is respected), then converted to world space.
            Vector3 randomLocal = spawnArea.center + new Vector3(
                Random.Range(-0.5f, 0.5f) * spawnArea.size.x,
                Random.Range(-0.5f, 0.5f) * spawnArea.size.y,
                Random.Range(-0.5f, 0.5f) * spawnArea.size.z
            );

            Vector3 randomWorld = spawnArea.transform.TransformPoint(randomLocal);

            if (NavMesh.SamplePosition(randomWorld, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    // ---------------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        BoxCollider box = spawnArea != null ? spawnArea : GetComponent<BoxCollider>();
        if (box == null)
            return;

        Gizmos.matrix = box.transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}