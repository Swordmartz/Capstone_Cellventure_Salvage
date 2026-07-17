using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Continuously spawns InfluenzaFSM enemies one at a time, drawing from a
// pre-warmed object pool instead of Instantiate/Destroy at runtime.
public class InfluenzaSpawner : MonoBehaviour
{
    [Header("Prefab & Pool")]
    [SerializeField] private InfluenzaFSM prefab;
    [SerializeField] private int poolSize = 20;

    [Header("Spawn Settings")]
    [SerializeField] private int maxAliveCount = 10;
    [SerializeField] private float spawnInterval = 3f;

    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private int navMeshSampleAreaMask = NavMesh.AllAreas;

    private readonly List<InfluenzaFSM> _pool = new List<InfluenzaFSM>();
    private float _timer;

    private void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError($"{nameof(InfluenzaSpawner)} on {name} has no prefab assigned.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            InfluenzaFSM instance = Instantiate(prefab, transform.position, Quaternion.identity);
            instance.gameObject.SetActive(false);
            _pool.Add(instance);
        }
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < spawnInterval)
            return;

        _timer = 0f;
        TrySpawnOne();
    }

    private void TrySpawnOne()
    {
        if (CountAlive() >= maxAliveCount)
            return;

        InfluenzaFSM instance = GetPooledInstance();
        if (instance == null)
        {
            // Pool exhausted - every instance is currently alive/in-use.
            return;
        }

        if (!TryGetSpawnPoint(out Vector3 point))
            return;

        instance.transform.position = point;
        instance.gameObject.SetActive(true);
        instance.ResetForSpawn();
    }

    private int CountAlive()
    {
        int count = 0;
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i] != null && _pool[i].gameObject.activeSelf)
                count++;
        }
        return count;
    }

    private InfluenzaFSM GetPooledInstance()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                return _pool[i];
        }
        return null;
    }

    private bool TryGetSpawnPoint(out Vector3 point)
    {
        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0f;
        Vector3 randomPoint = transform.position + randomOffset;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, spawnRadius, navMeshSampleAreaMask))
        {
            point = hit.position;
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}