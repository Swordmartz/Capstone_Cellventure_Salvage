using System.Collections.Generic;
using UnityEngine;

// Combined object pool + spawner for enemies.
// Pre-instantiates a pool of enemies, places initial ones at spawn points,
// and hands out/reclaims enemies via Spawn()/Return() so the FSM's cloning
// and reuse logic doesn't need a separate pool manager.
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Pool Settings")]
    public GameObject enemyPrefab;
    public int initialPoolSize = 10;
    public bool expandable = true; // if true, creates a new instance when the pool runs dry

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Initial Spawn")]
    public int enemiesToSpawnAtStart = 5;
    public float delayBetweenInitialSpawns = 1f; // time between each initial spawn

    [Header("Continuous Spawning (optional)")]
    public bool spawnOverTime = false;
    public float spawnInterval = 10f;
    public int maxActiveEnemies = 20;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private int currentSpawnedCount = 0;
    private float spawnTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject obj = Instantiate(enemyPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    void Start()
    {
        StartCoroutine(SpawnInitialEnemies());
    }

    private System.Collections.IEnumerator SpawnInitialEnemies()
    {
        for (int i = 0; i < enemiesToSpawnAtStart; i++)
        {
            SpawnAtRandomPoint();
            yield return new WaitForSeconds(delayBetweenInitialSpawns);
        }
    }

    void Update()
    {
        if (!spawnOverTime) return;
        if (currentSpawnedCount >= maxActiveEnemies) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnAtRandomPoint();
            spawnTimer = spawnInterval;
        }
    }

    // ------------------ Spawn Point Placement ------------------

    public void SpawnAtRandomPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned to EnemySpawner!");
            return;
        }

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Spawn(point.position, point.rotation);

        if (enemy != null)
        {
            currentSpawnedCount++;
        }
    }

    // ------------------ Pool Core ------------------

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else if (expandable)
        {
            obj = Instantiate(enemyPrefab, transform);
        }
        else
        {
            Debug.LogWarning("EnemySpawner pool is empty and not expandable — no enemy spawned.");
            return null;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        // Reset the FSM's internal state so a reused enemy behaves like a fresh one
        pneumonococcalFSM fsm = obj.GetComponent<pneumonococcalFSM>();
        if (fsm != null)
        {
            fsm.ResetForReuse();
        }

        return obj;
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pool.Enqueue(obj);
    }
}