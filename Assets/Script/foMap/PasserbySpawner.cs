using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class PasserbySpawner : MonoBehaviour
{
    [Header("Spline")]
    public SplineContainer splineContainer;

    [Header("Passerby Prefabs")]
    public List<GameObject> passerbyPrefabs = new List<GameObject>();

    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    public bool loopList = true;

    [Header("Speed Settings")]
    public float minSpeed = 0.5f;
    public float maxSpeed = 2f;

    private int _lastIndex = -1;
    private Coroutine _spawnRoutine;

    void Start()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        if (passerbyPrefabs == null || passerbyPrefabs.Count == 0)
        {
            Debug.LogWarning("PasserbySpawner: No prefabs assigned.");
            return;
        }

        _spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
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
            Debug.LogWarning($"PasserbySpawner: Prefab at index {newIndex} is null, skipping.");
            return;
        }

        Vector3 spawnPos = splineContainer != null
            ? splineContainer.transform.TransformPoint(
                SplineUtility.EvaluatePosition(splineContainer.Spline, 0f))
            : transform.position;

        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity);

        PasserbySplinePath path = instance.GetComponent<PasserbySplinePath>();
        if (path != null)
        {
            path.splineContainer = splineContainer;
            path.speed = Random.Range(minSpeed, maxSpeed);
        }
        else
        {
            Debug.LogWarning($"PasserbySpawner: Prefab '{prefab.name}' has no PasserbySplinePath component.");
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