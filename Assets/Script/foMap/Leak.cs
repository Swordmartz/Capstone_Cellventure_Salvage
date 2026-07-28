using UnityEngine;

/// <summary>
/// Attach to a "leak" object. While this object is enabled, it counts toward
/// the Inflammation meter. Call Seal() when the player fixes the leak (or
/// just destroy/disable the GameObject) to remove its contribution.
/// </summary>
public class LeakSource : MonoBehaviour
{
    [Tooltip("How much this leak counts toward inflammation relative to other sources.")]
    [SerializeField, Min(0f)] private float inflammationWeight = 1f;

    public bool IsSealed { get; private set; }

    private void OnEnable()
    {
        if (IsSealed) return;

        if (InflammationManager.Instance != null)
            InflammationManager.Instance.RegisterSource(this, inflammationWeight);
    }

    private void OnDisable()
    {
        if (InflammationManager.Instance != null)
            InflammationManager.Instance.UnregisterSource(this);
    }

    /// <summary>
    /// Call this when the leak is fixed. Keeps the GameObject around (in case
    /// you want to show a "sealed" visual state) but stops it contributing to
    /// inflammation.
    /// </summary>
    public void Seal()
    {
        if (IsSealed) return;

        IsSealed = true;

        if (InflammationManager.Instance != null)
            InflammationManager.Instance.UnregisterSource(this);
    }

    /// <summary>Re-opens a sealed leak (e.g. it breaks open again later).</summary>
    public void Reopen()
    {
        if (!IsSealed) return;

        IsSealed = false;

        if (isActiveAndEnabled && InflammationManager.Instance != null)
            InflammationManager.Instance.RegisterSource(this, inflammationWeight);
    }
}