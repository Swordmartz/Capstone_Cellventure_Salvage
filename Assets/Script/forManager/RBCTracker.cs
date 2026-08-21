using UnityEngine;
using System;

/// <summary>
/// Central tracker for red blood cell counts, and optionally enemy counts.
/// Attach this to a single persistent GameObject in your scene (e.g. an
/// empty "Managers" object).
///
/// Call these methods from wherever you spawn / infect / cure RBCs:
///   RBCTracker.Instance.RegisterRBC()      -> when a healthy RBC is created
///   RBCTracker.Instance.RegisterInfection()-> when a healthy RBC becomes infected
///   RBCTracker.Instance.RegisterCure()     -> when an infected RBC is cured/killed
///   RBCTracker.Instance.RegisterRBCDeath() -> when any RBC is removed entirely
///
/// And optionally, from wherever you spawn / kill enemies:
///   RBCTracker.Instance.RegisterEnemySpawned()  -> when an enemy is created
///   RBCTracker.Instance.RegisterEnemyDefeated() -> when an enemy is killed/removed
/// </summary>
public class RBCTracker : MonoBehaviour
{
    public static RBCTracker Instance { get; private set; }

    [Header("Live Counts (read-only, for debugging)")]
    [SerializeField] private int totalRBC = 0;
    [SerializeField] private int infectedRBC = 0;

    [Header("Enemy Counts (read-only, for debugging)")]
    [Tooltip("Total enemies registered via RegisterEnemySpawned() so far this level, or the peak " +
             "count seen by the auto-scan below if that's enabled.")]
    [SerializeField] private int totalEnemies = 0;
    [Tooltip("Enemies still alive/remaining right now.")]
    [SerializeField] private int remainingEnemies = 0;

    [Header("Enemy Layer Auto-Scan")]
    [Tooltip("If true, enemy counts are counted automatically each scanInterval seconds by scanning " +
             "for colliders on enemyLayer, instead of requiring manual RegisterEnemySpawned()/" +
             "RegisterEnemyDefeated() calls from elsewhere. This is the easiest way to get enemy " +
             "counts working without having to wire up every enemy script individually.")]
    [SerializeField] private bool autoScanEnemyLayer = true;

    [Tooltip("Layer(s) enemies are on. Should match whatever layer your enemy colliders use " +
             "elsewhere (e.g. NeutrophilNPCAlly's Enemy Layer field).")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("If true, the auto-scan searches the ENTIRE scene for colliders on enemyLayer - no " +
             "distance restriction, scanCenter/scanRadius below are ignored. Simplest to set up " +
             "correctly (nothing can spawn 'outside' it), but checks every collider in the scene each " +
             "scan, so it's more expensive on very large/busy scenes. Turn this off to use the " +
             "bounded scanCenter/scanRadius sphere instead if that becomes a performance concern.")]
    [SerializeField] private bool scanGlobally = true;

    [Tooltip("Center of the scan sphere. Only used when scanGlobally is off. If left empty, defaults " +
             "to this GameObject's own position - set this to something like the level center (or " +
             "leave a large enough scanRadius) so the scan actually covers the whole playable area " +
             "regardless of where this tracker sits.")]
    [SerializeField] private Transform scanCenter;

    [Tooltip("Radius of the scan sphere. Only used when scanGlobally is off - needs to be large " +
             "enough to cover the entire area enemies can be in, or ones outside this radius won't " +
             "be counted.")]
    [SerializeField] private float scanRadius = 100f;

    [Tooltip("How often (in seconds) the auto-scan re-counts enemies.")]
    [SerializeField] private float scanInterval = 0.5f;

    private float scanTimer;

    [Range(0f, 1f)]
    [Tooltip("How much the remaining-enemy fraction contributes to InfectionPercentage, blended " +
             "with the RBC infection ratio (0 = enemies ignored entirely, 1 = enemies only). This " +
             "only takes effect once at least one enemy has been registered via " +
             "RegisterEnemySpawned() - until then InfectionPercentage is exactly the RBC ratio, same " +
             "as before, so existing thresholds (e.g. MalariaWinCondition) aren't silently changed by " +
             "adding this field.")]
    [SerializeField] private float enemyWeight = 0.5f;

    [Header("Infection Slider UI")]
    [Tooltip("Optional. If assigned, this slider's value is kept in sync with InfectionPercentage " +
             "(0-1 range) any time the counts change.")]
    [SerializeField] private UnityEngine.UI.Slider infectionSlider;

    /// <summary>Fired any time totalRBC, infectedRBC, totalEnemies, or remainingEnemies changes.</summary>
    public event Action OnCountsChanged;

    public int TotalRBC => totalRBC;
    public int InfectedRBC => infectedRBC;

    public int TotalEnemies => totalEnemies;
    public int RemainingEnemies => remainingEnemies;

    /// <summary>RBC infection rate as a 0-1 fraction, ignoring enemies entirely. Returns 0 if there are no RBCs at all.</summary>
    public float RbcInfectionFraction => totalRBC <= 0 ? 0f : (float)infectedRBC / totalRBC;

    /// <summary>Fraction of enemies still remaining (0-1). Returns 0 if no enemies have been registered.</summary>
    public float EnemyPercentage => totalEnemies <= 0 ? 0f : (float)remainingEnemies / totalEnemies;

    /// <summary>
    /// Combined 0-1 infection bar value. Until any enemy is registered via
    /// RegisterEnemySpawned(), this is exactly RbcInfectionFraction (same
    /// behavior as before enemy tracking existed). Once enemies are being
    /// tracked, it's a blend of the RBC infection ratio and the remaining-
    /// enemy fraction, weighted by enemyWeight.
    /// </summary>
    public float InfectionPercentage
    {
        get
        {
            if (totalEnemies <= 0)
                return RbcInfectionFraction;

            return Mathf.Lerp(RbcInfectionFraction, EnemyPercentage, enemyWeight);
        }
    }

    private void Awake()
    {
        // Simple singleton so any script can reach this via RBCTracker.Instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (infectionSlider != null)
        {
            infectionSlider.minValue = 0f;
            infectionSlider.maxValue = 1f;
        }

        UpdateSlider();

        if (autoScanEnemyLayer)
        {
            // Do an immediate scan on start rather than waiting for the
            // first scanInterval to elapse, so counts aren't sitting at 0
            // for the first fraction of a second.
            ScanEnemyLayer();
        }
    }

    private void Update()
    {
        if (!autoScanEnemyLayer)
            return;

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            ScanEnemyLayer();
        }
    }

    // Counts live colliders on enemyLayer, either scene-wide (scanGlobally)
    // or within scanRadius of scanCenter. remainingEnemies is set directly
    // to the current count; totalEnemies tracks the highest count ever
    // seen, which approximates "how many enemies existed at the start" as
    // long as enemies are all present (or spawn in) before any of them
    // start dying - if enemies trickle in gradually throughout play,
    // totalEnemies will keep climbing as new spawns are seen instead of
    // representing a fixed starting count. For an exact known starting
    // count instead, call SetEnemyCounts() once with autoScanEnemyLayer off.
    private void ScanEnemyLayer()
    {
        int count = scanGlobally ? CountEnemiesGlobally() : CountEnemiesInRadius();

        remainingEnemies = count;
        totalEnemies = Mathf.Max(totalEnemies, remainingEnemies);

        NotifyChanged();
    }

    // Scene-wide search - checks every collider currently loaded against
    // enemyLayer via a bitmask comparison. No position/distance involved,
    // so nothing can spawn "outside" this and go uncounted.
    private int CountEnemiesGlobally()
    {
        Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        int count = 0;

        foreach (Collider col in allColliders)
        {
            if (((1 << col.gameObject.layer) & enemyLayer.value) != 0)
                count++;
        }

        return count;
    }

    private int CountEnemiesInRadius()
    {
        Vector3 center = scanCenter != null ? scanCenter.position : transform.position;
        return Physics.OverlapSphere(center, scanRadius, enemyLayer).Length;
    }

    public void RegisterRBC()
    {
        totalRBC++;
        NotifyChanged();
    }

    public void RegisterInfection()
    {
        infectedRBC = Mathf.Clamp(infectedRBC + 1, 0, totalRBC);
        NotifyChanged();
    }

    public void RegisterCure()
    {
        infectedRBC = Mathf.Max(0, infectedRBC - 1);
        NotifyChanged();
    }

    public void RegisterRBCDeath(bool wasInfected)
    {
        totalRBC = Mathf.Max(0, totalRBC - 1);
        if (wasInfected)
            infectedRBC = Mathf.Max(0, infectedRBC - 1);
        NotifyChanged();
    }

    /// <summary>
    /// Call when an enemy spawns/is created. Only use this if autoScanEnemyLayer
    /// is OFF - otherwise the next scan will just overwrite these counts
    /// with whatever it currently finds on enemyLayer, and the two will fight.
    /// </summary>
    public void RegisterEnemySpawned()
    {
        totalEnemies++;
        remainingEnemies++;
        NotifyChanged();
    }

    /// <summary>
    /// Call when an enemy is killed/removed. Only use this if autoScanEnemyLayer
    /// is OFF, for the same reason as RegisterEnemySpawned().
    /// </summary>
    public void RegisterEnemyDefeated()
    {
        remainingEnemies = Mathf.Max(0, remainingEnemies - 1);
        NotifyChanged();
    }

    /// <summary>
    /// Use this instead of the incremental enemy methods if it's easier to
    /// just recompute counts yourself (e.g. by counting a List of enemy
    /// objects each frame).
    /// </summary>
    public void SetEnemyCounts(int total, int remaining)
    {
        totalEnemies = Mathf.Max(0, total);
        remainingEnemies = Mathf.Clamp(remaining, 0, totalEnemies);
        NotifyChanged();
    }

    /// <summary>
    /// Use this instead of the incremental methods if it's easier to just
    /// recompute counts yourself (e.g. by counting a List of RBC objects each frame).
    /// </summary>
    public void SetCounts(int total, int infected)
    {
        totalRBC = Mathf.Max(0, total);
        infectedRBC = Mathf.Clamp(infected, 0, totalRBC);
        NotifyChanged();
    }

    private void UpdateSlider()
    {
        if (infectionSlider != null)
            infectionSlider.value = InfectionPercentage;
    }

    private void NotifyChanged()
    {
        UpdateSlider();
        OnCountsChanged?.Invoke();
    }
}