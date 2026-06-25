using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class PasserbySpawnerB : MonoBehaviour
{
    [Header("Spline")]
    public SplineContainer splineContainer;

    [Header("Passerby Prefabs")]
    public List<GameObject> passerbyPrefabs = new List<GameObject>();

    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    [Tooltip("Delay before the very first spawn, so the scene doesn't pop a passerby immediately on Start.")]
    public float initialDelay = 2f;
    public bool loopList = true;

    [Header("Speed Settings")]
    public float minSpeed = 0.5f;
    public float maxSpeed = 2f;

    [Header("Pool Settings")]
    [Tooltip("How many instances to pre-create per prefab at startup.")]
    public int poolSizePerPrefab = 5;

    private int _lastIndex = -1;
    private Coroutine _spawnRoutine;

    // Pool: one queue per prefab index
    private Dictionary<int, Queue<GameObject>> _pool = new Dictionary<int, Queue<GameObject>>();

    void Start()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        if (passerbyPrefabs == null || passerbyPrefabs.Count == 0)
        {
            Debug.LogWarning("PasserbySpawnerB: No prefabs assigned.");
            return;
        }

        WarmUpPool();

        _spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    // ── Pool ──────────────────────────────────────────────────────────────────

    void WarmUpPool()
    {
        for (int i = 0; i < passerbyPrefabs.Count; i++)
        {
            if (passerbyPrefabs[i] == null) continue;

            _pool[i] = new Queue<GameObject>();

            for (int j = 0; j < poolSizePerPrefab; j++)
            {
                GameObject obj = CreateInstance(passerbyPrefabs[i]);
                obj.SetActive(false);
                _pool[i].Enqueue(obj);
            }
        }
    }

    GameObject CreateInstance(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        // Listen for when PasserbySplinePathB finishes so we can return to pool
        PasserbySplinePathB path = obj.GetComponent<PasserbySplinePathB>();
        if (path != null)
            path.OnPathFinished += () => ReturnToPool(obj);
        return obj;
    }

    GameObject GetFromPool(int index)
    {
        if (!_pool.ContainsKey(index))
            _pool[index] = new Queue<GameObject>();

        // If pool is empty, create a new one on the fly
        if (_pool[index].Count == 0)
        {
            Debug.Log($"PasserbySpawnerB: Pool empty for index {index}, creating new instance.");
            return CreateInstance(passerbyPrefabs[index]);
        }

        return _pool[index].Dequeue();
    }

    void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);

        // Find which prefab index this belongs to
        for (int i = 0; i < passerbyPrefabs.Count; i++)
        {
            if (passerbyPrefabs[i] == null) continue;
            if (obj.name.Contains(passerbyPrefabs[i].name))
            {
                _pool[i].Enqueue(obj);
                return;
            }
        }
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    IEnumerator SpawnRoutine()
    {
        // Wait before the first spawn instead of spawning immediately on Start.
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (loopList)
        {
            SpawnNext();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnNext()
    {
        if (passerbyPrefabs.Count == 0) return;

        int newIndex = GetRandomIndex();
        GameObject prefab = passerbyPrefabs[newIndex];
        _lastIndex = newIndex;

        if (prefab == null)
        {
            Debug.LogWarning($"PasserbySpawnerB: Prefab at index {newIndex} is null, skipping.");
            return;
        }

        Vector3 spawnPos = splineContainer != null
            ? splineContainer.transform.TransformPoint(
                SplineUtility.EvaluatePosition(splineContainer.Spline, 0f))
            : transform.position;

        // Get from pool instead of Instantiate
        GameObject instance = GetFromPool(newIndex);
        instance.transform.position = spawnPos;
        instance.transform.rotation = Quaternion.identity;
        instance.SetActive(true);

        PasserbySplinePathB path = instance.GetComponent<PasserbySplinePathB>();
        if (path != null)
        {
            path.splineContainer = splineContainer;
            path.speed = Random.Range(minSpeed, maxSpeed);
            path.ResetPath(); // make sure it starts from beginning
        }
        else
        {
            Debug.LogWarning($"PasserbySpawnerB: Prefab '{prefab.name}' has no PasserbySplinePathB component.");
        }
    }

    int GetRandomIndex()
    {
        if (passerbyPrefabs.Count == 1) return 0;

        int index;
        do
        {
            index = Random.Range(0, passerbyPrefabs.Count);
        }
        while (index == _lastIndex);

        return index;
    }

    // ── Controls ──────────────────────────────────────────────────────────────

    public void StopSpawning()
    {
        if (_spawnRoutine != null)
            StopCoroutine(_spawnRoutine);
    }

    public void RestartSpawning()
    {
        StopSpawning();
        _lastIndex = -1;
        _spawnRoutine = StartCoroutine(SpawnRoutine());
    }
}