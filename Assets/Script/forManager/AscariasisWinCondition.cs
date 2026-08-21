using UnityEngine;

/// <summary>
/// Watches an EnemyPatrolFSM and triggers the win state (disable gameplay UI,
/// activate the win screen) once it reaches its Dead state.
///
/// Two ways to hook this up — use whichever fits your scene, both work
/// together safely if you happen to wire up both:
///
///   A) POLLING (default, needs no changes to EnemyPatrolFSM):
///      Drag the boss/target enemy into the `enemy` field below (or call
///      AssignEnemy() once your spawner instantiates it at runtime). This
///      script checks enemy.IsDead every frame and fires the win once.
///      Nothing is checked until `enemy` is actually assigned, so if the
///      enemy spawns in later, there's no chance of an instant win.
///
///   B) EVENT-DRIVEN (zero per-frame checking):
///      EnemyPatrolFSM already calls objectToActivateOnDeath.SetActive(true)
///      from its Die() method. Put THIS script on its own GameObject, leave
///      that GameObject DISABLED in the scene by default, and drag it into
///      the enemy's "Object To Activate On Death" field in the Inspector.
///      When the enemy dies, Unity activates this GameObject, OnEnable()
///      fires, and the win triggers immediately with no polling at all.
///      (Leave `enemy` unassigned in this setup — that's how this script
///      tells Option A and Option B apart.)
///
/// GUARD ADDED: previously, if the GameObject for Option B was accidentally
/// left ACTIVE in the scene (instead of starting disabled), OnEnable() fired
/// at scene load — before the enemy had done anything — and immediately
/// scored/won using whatever raw values happened to be sitting in
/// ValuesForStar's Inspector fields at that moment. That produced a frozen,
/// always-the-same score no matter how the player performed. This version
/// refuses to treat an OnEnable that fires during scene load as a real death
/// event, and logs a loud error telling you exactly what to fix instead of
/// silently scoring garbage.
/// </summary>
public class AscariasisWinCondition : MonoBehaviour
{
    [Header("Enemy To Watch (Option A: polling)")]
    [Tooltip("Optional if you're using the event-driven setup (Option B) instead. Leave empty if this GameObject is being activated directly via EnemyPatrolFSM's objectToActivateOnDeath, OR if the enemy is spawned at runtime and will be assigned later via AssignEnemy().")]
    [SerializeField] private EnemyPatrolFSM enemy;

    [Header("Win UI")]
    [Tooltip("Gameplay UI to hide once the win condition is met (e.g. health bars, HUD).")]
    [SerializeField] private GameObject uiToDisable;

    [Tooltip("Win screen to show once the win condition is met.")]
    [SerializeField] private GameObject winScreen;

    [Header("Star Rating")]
    [Tooltip("StarRatingManager that should evaluate and display the score/stars once " +
             "the win condition fires. This is usually a component on (or under) " +
             "winScreen. Leave empty if this win condition doesn't need a star rating.")]
    [SerializeField] private StarRatingManager starRatingManager;
    [SerializeField] private ValuesForStar rating;

    [Tooltip("Mission data to pass into StarRatingManager.EvaluateFromMission(). " +
             "Only relevant if StarRatingManager's useFormula1 is checked — if it's using " +
             "the Ascariasis formula instead (reads its own ValuesForStar reference), " +
             "this can be left unassigned.")]
    [SerializeField] private AI_TestTD missionData;

    [Header("Safety Guard")]
    [Tooltip("Option B win triggers (OnEnable) are ignored if they happen within this many " +
             "seconds of the scene loading. This catches the case where the win-condition " +
             "GameObject was accidentally left active in the scene instead of starting " +
             "disabled — without this guard, that misconfiguration silently fires an instant, " +
             "always-the-same win/score at scene start.")]
    [SerializeField] private float earlyTriggerGuardSeconds = 0.25f;

    // Guards against firing twice, whether from OnEnable, Update polling,
    // or both if this ends up wired up both ways at once.
    private bool winTriggered;

    // Timestamp this component became active in memory (Awake), used to tell
    // "activated because the scene just loaded with this object active" apart
    // from "activated later, by EnemyPatrolFSM.Die() calling SetActive(true)".
    private float awakeRealtime;

    private void Awake()
    {
        awakeRealtime = Time.realtimeSinceStartup;
    }

    private void OnValidate()
    {
        // Editor-time sanity check for Option B: if `enemy` is unassigned
        // (meaning this is meant to be event-driven) but the GameObject is
        // active in the scene, that's the exact misconfiguration that causes
        // an instant, wrong-looking win the moment the scene loads.
        if (enemy == null && gameObject.activeSelf)
        {
            Debug.LogWarning($"[AscariasisWinCondition] '{gameObject.name}' has no `enemy` " +
                "assigned (Option B / event-driven setup) but its GameObject is ACTIVE in the " +
                "scene. It should start DISABLED and be activated only via " +
                "EnemyPatrolFSM's 'Object To Activate On Death' field, or it will fire an " +
                "instant win/score at scene load using whatever stale values are currently on " +
                "ValuesForStar. Either uncheck this GameObject in the Hierarchy, or assign " +
                "`enemy` if you actually meant to use Option A (polling).");
        }
    }

    private void OnEnable()
    {
        // Only auto-trigger here for Option B (pure event-driven setup),
        // where this GameObject starts disabled and gets SetActive(true)
        // directly from EnemyPatrolFSM.Die().
        if (enemy != null)
            return; // Option A (polling) — Update() handles it, not OnEnable.

        // Guard: if this OnEnable is firing essentially at the same moment
        // Awake() did (i.e. the object was active from scene start rather
        // than being switched on later by Die()), treat it as a misconfig,
        // not a real death event.
        float sinceAwake = Time.realtimeSinceStartup - awakeRealtime;
        if (sinceAwake <= earlyTriggerGuardSeconds)
        {
            Debug.LogError($"[AscariasisWinCondition] '{gameObject.name}' fired OnEnable " +
                $"{sinceAwake:0.###}s after Awake — this looks like the GameObject was active " +
                "in the scene at load time rather than being activated later by " +
                "EnemyPatrolFSM.Die(). Ignoring this as a win trigger to avoid scoring with " +
                "stale/default ValuesForStar data. Fix: leave this GameObject DISABLED in the " +
                "scene and only let EnemyPatrolFSM activate it on death.");
            return;
        }

        TryTriggerWin();
    }

    private void Update()
    {
        // Covers Option A: keep checking the assigned enemy's death state.
        // Cheap early-out means this is a no-op until an enemy reference
        // exists — so if the enemy spawns in later (via AssignEnemy), no
        // checking happens, and no win, until that assignment occurs.
        if (winTriggered || enemy == null)
            return;

        if (enemy.IsDead)
            TryTriggerWin();
    }

    private void TryTriggerWin()
    {
        if (winTriggered)
            return;

        winTriggered = true;

        // Hide player UI
        if (uiToDisable != null)
            uiToDisable.SetActive(false);

        // Show results screen
        if (winScreen != null)
            winScreen.SetActive(true);

        // Evaluate and display star rating — this reads whatever RBC/WBC/ICE
        // values ValuesForStar has accumulated from ReportOxygenDeliver /
        // ReportEnemyKilled / ReportBarValue over the course of the actual
        // playthrough, since TryTriggerWin now only ever runs after a real
        // death event (Option A's enemy.IsDead, or Option B's guarded OnEnable).
        if (starRatingManager != null && rating != null)
            starRatingManager.EvaluateScore(rating.OxygenDeliver, rating.EnemyKilled);
    }

    /// <summary>
    /// Call this from your enemy spawner right after Instantiate() to hook
    /// up Option A (polling) for enemies that don't exist in the scene yet
    /// at load time. Win checks only begin once this is called, so there's
    /// no window for an instant win before the enemy actually spawns.
    /// </summary>
    public void AssignEnemy(EnemyPatrolFSM spawnedEnemy)
    {
        enemy = spawnedEnemy;
    }

    /// <summary>
    /// Public in case you'd rather trigger the win from somewhere else
    /// entirely (a different event, a manual test button, etc.) instead of
    /// either built-in option above. This bypasses the early-trigger guard
    /// entirely since it's an explicit, intentional call.
    /// </summary>
    public void TriggerWin()
    {
        TryTriggerWin();
    }
}