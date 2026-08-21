using UnityEngine;

/// <summary>
/// Pure storage - no calculation logic. Other scripts write into this via
/// the Report___ methods below as the level plays out, and whatever script
/// actually computes the star rating reads the values back out via the
/// public properties (or ResetValues() to clear them for a new attempt).
/// </summary>
public class ValuesForStar : MonoBehaviour
{
    public static ValuesForStar Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [field: Header("RBC")]
    [field: Tooltip("Amount of oxygen delivered so far this attempt.")]
    [field: SerializeField] public float OxygenDeliver { get; private set; }

    [field: Header("WBC")]
    [field: Tooltip("Number of enemies killed so far this attempt.")]
    [field: SerializeField] public int EnemyKilled { get; private set; }

    [field: Header("Platelets")]
    [field: Tooltip("Number of wounds healed so far this attempt.")]
    [field: SerializeField] public int WoundHealed { get; private set; }

    [field: Header("IEC")]
    [field: Tooltip("Current IEC bar value for this attempt.")]
    [field: SerializeField] public float BarValue { get; private set; }

    /// <summary>Called by whatever tracks oxygen delivery (e.g. RBCTracker).</summary>
    public void ReportOxygenDeliver(float amount)
    {
        OxygenDeliver = amount;
    }

    /// <summary>Called each time an enemy is killed (e.g. from WBC combat script).</summary>
    public void ReportEnemyKilled()
    {
        EnemyKilled++;
    }

    /// <summary>Called each time a wound is healed (e.g. from a Platelet script).</summary>
    public void ReportWoundHealed()
    {
        WoundHealed++;
    }

    /// <summary>Called by whatever tracks the ICE bar to update its current value.</summary>
    public void ReportBarValue(float value)
    {
        BarValue = value;
    }

    /// <summary>Clears all reported values, ready for a fresh attempt at the level.</summary>
    public void ResetValues()
    {
        OxygenDeliver = 0f;
        EnemyKilled = 0;
        WoundHealed = 0;
        BarValue = 0f;
    }
}