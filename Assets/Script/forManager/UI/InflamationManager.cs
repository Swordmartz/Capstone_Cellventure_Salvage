using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central place anything can register/unregister itself with to affect the
/// Inflammation meter. Leaks, enemies, and infected cells all call
/// RegisterSource(this) while they're "a problem" and UnregisterSource(this)
/// when they're fixed/killed/cured — the bar smoothly follows the weighted
/// count of whatever's currently registered.
///
/// Infection is tracked separately (its own manager) — this class only
/// handles Inflammation.
/// </summary>
public class InflammationManager : MonoBehaviour
{
    public static InflammationManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Slider inflammationSlider;

    [Header("Inflammation Settings")]
    [Tooltip("The total weighted sources needed for the meter to read fully inflamed. E.g. if this is 10 and each source has weight 1, 10 simultaneous sources = fully inflamed.")]
    [SerializeField, Min(0.01f)] private float sourceWeightForFullMeter = 10f;

    [Tooltip("How quickly the bar visually moves toward its target value (0-1 scale, per second). Higher = snappier.")]
    [SerializeField, Min(0.01f)] private float smoothSpeed = 1.5f;

    private readonly Dictionary<Object, float> activeSources = new Dictionary<Object, float>();
    private float weightedSourceTotal;
    private float currentNormalizedInflammation;

    /// <summary>Current inflammation, 0 (none) to 1 (fully inflamed).</summary>
    public float NormalizedInflammation => currentNormalizedInflammation;

    /// <summary>How many distinct sources (leaks/enemies/infected cells/etc.) are currently registered.</summary>
    public int ActiveSourceCount => activeSources.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        float target = Mathf.Clamp01(weightedSourceTotal / sourceWeightForFullMeter);
        currentNormalizedInflammation = Mathf.MoveTowards(
            currentNormalizedInflammation,
            target,
            smoothSpeed * Time.deltaTime);

        if (inflammationSlider != null)
            inflammationSlider.value = currentNormalizedInflammation;
    }

    /// <summary>
    /// Call while a leak/enemy/infected cell/etc. is active and should be
    /// pushing inflammation up. Safe to call repeatedly — a source already
    /// registered is ignored.
    /// </summary>
    /// <param name="source">Typically `this` from the calling component.</param>
    /// <param name="weight">How much this source counts for, relative to others. Default 1.</param>
    public void RegisterSource(Object source, float weight = 1f)
    {
        if (source == null) return;
        if (activeSources.ContainsKey(source)) return;

        activeSources[source] = weight;
        weightedSourceTotal += weight;
    }

    /// <summary>
    /// Call when a leak is sealed, an enemy dies, or a cell is cured — removes
    /// its contribution so the meter can come back down. Safe to call even if
    /// the source was never registered.
    /// </summary>
    public void UnregisterSource(Object source)
    {
        if (source == null) return;

        if (activeSources.TryGetValue(source, out float weight))
        {
            activeSources.Remove(source);
            weightedSourceTotal -= weight;
        }
    }
}