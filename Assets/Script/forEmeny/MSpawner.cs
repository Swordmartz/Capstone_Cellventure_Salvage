using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Spawns a prefab onto a randomly chosen spline out of a list of assignable
/// splines. Each spawn places the object at the start (t=0) of the chosen
/// spline, then hands that spline off to the prefab's RBCSplineSpriteSwitcher
/// component (via AssignSpline) so it knows what to follow from there.
/// </summary>
public class SplineSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SplinePath
    {
        public SplineContainer container;

        [Min(0)]
        public int splineIndex;
    }

    [Header("Prefab")]
    [Tooltip("The object to spawn. Must have an RBCSplineSpriteSwitcher component so the chosen spline can be assigned to it.")]
    [SerializeField] private GameObject prefab;

    [Header("Spline Paths")]
    [Tooltip("Assign as many splines as you want — one is picked at random for each spawn.")]
    [SerializeField] private SplinePath[] splinePaths;

    [Header("Spawning")]
    [SerializeField, Min(0.02f)] private float spawnInterval = 2f;
    [SerializeField, Min(1)] private int maxAlive = 10;
    [SerializeField] private bool autoStart = true;

    private readonly List<GameObject> activeSpawns = new List<GameObject>();
    private Coroutine spawnRoutine;

    public int ActiveCount => activeSpawns.Count;

    private void Start()
    {
        if (autoStart)
            StartSpawning();
    }

    private void OnDestroy()
    {
        StopSpawning();
    }

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.02f, spawnInterval);
        maxAlive = Mathf.Max(1, maxAlive);
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

    /// <summary>
    /// Picks a random assigned spline, spawns the prefab at its t=0 point,
    /// assigns that spline to the prefab's RBC switcher, and returns the new
    /// instance (or null if spawning wasn't possible right now — see the
    /// console for why).
    /// </summary>
    public GameObject SpawnOne()
    {
        if (!CanSpawn())
            return null;

        SplinePath path = splinePaths[Random.Range(0, splinePaths.Length)];

        if (!IsValidSplinePath(path))
        {
            Debug.LogWarning("[SplineSpawner] The selected spline path is invalid.", this);
            return null;
        }

        Vector3 spawnPosition = path.container.EvaluatePosition(path.splineIndex, 0f);
        GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);

        // Hand the chosen spline off to RBC so it knows what to follow.
        RBCSplineSpriteSwitcher rbc = instance.GetComponent<RBCSplineSpriteSwitcher>();
        if (rbc != null)
        {
            rbc.AssignSpline(path.container, path.splineIndex);
        }
        else
        {
            Debug.LogWarning("[SplineSpawner] Spawned prefab has no RBCSplineSpriteSwitcher component to assign the spline to.", instance);
        }

        activeSpawns.Add(instance);
        return instance;
    }

    private bool CanSpawn()
    {
        if (prefab == null)
        {
            Debug.LogWarning("[SplineSpawner] No prefab is assigned.", this);
            return false;
        }

        if (splinePaths == null || splinePaths.Length == 0)
        {
            Debug.LogWarning("[SplineSpawner] No spline paths are assigned.", this);
            return false;
        }

        return true;
    }

    private bool IsValidSplinePath(SplinePath path)
    {
        return path.container != null &&
               path.splineIndex >= 0 &&
               path.splineIndex < path.container.Splines.Count;
    }

    private IEnumerator SpawnLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(spawnInterval);

        while (true)
        {
            // Drop any destroyed instances so the count stays accurate.
            activeSpawns.RemoveAll(go => go == null);

            if (activeSpawns.Count < maxAlive)
                SpawnOne();

            yield return wait;
        }
    }
}