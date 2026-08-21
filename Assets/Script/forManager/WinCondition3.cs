using UnityEngine;

/// <summary>
/// Win condition that triggers once there are no more active enemies left on
/// a given physics layer (rather than watching a single tracked enemy's
/// death state, like AscariasisWinCondition does).
///
/// Checks on a timer (not every frame -- scanning all colliders in the scene
/// every frame is wasteful) and only starts counting a "win" once at least
/// one enemy on the layer has actually been seen. That guard exists so this
/// doesn't fire instantly on scene load, before your spawner has had a
/// chance to spawn anything yet -- the layer being empty for zero enemies
/// spawned is not the same as the layer being empty because they're all dead.
/// </summary>
public class Level3WinCondition : MonoBehaviour
{
    public StarRatingManager starRatingManager;
    public ValuesForStar rating;

    [Header("Enemy Layer To Watch")]
    [Tooltip("Layer(s) enemies live on. Win triggers once no active colliders remain on this layer.")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Timing")]
    [Tooltip("How often (in seconds) to re-scan for remaining enemies. Lower = more responsive, higher = cheaper.")]
    [SerializeField] private float checkInterval = 0.5f;

    [Tooltip("If true, waits until at least one enemy on the layer has been seen before it will ever declare a win -- prevents an instant win if this checks before your spawner has placed any enemies yet.")]
    [SerializeField] private bool waitForFirstEnemy = true;

    [Header("Win UI")]
    [Tooltip("Gameplay UI to hide once the win condition is met (e.g. health bars, HUD).")]
    [SerializeField] private GameObject uiToDisable;

    [Tooltip("Win screen to show once the win condition is met.")]
    [SerializeField] private GameObject winScreen;

    private float checkTimer;
    private bool hasSeenEnemy;
    private bool winTriggered;

    private void Update()
    {
        if (winTriggered)
            return;

        checkTimer -= Time.deltaTime;
        if (checkTimer > 0f)
            return;

        checkTimer = checkInterval;

        int remaining = CountActiveEnemiesOnLayer();

        if (remaining > 0)
        {
            hasSeenEnemy = true;
            return;
        }

        // remaining == 0 from here on
        if (waitForFirstEnemy && !hasSeenEnemy)
            return; // never saw an enemy yet -- don't win on an empty-by-default layer

        TriggerWin();
    }

    private int CountActiveEnemiesOnLayer()
    {
        // FindObjectsByType only returns active-in-hierarchy objects by default,
        // so deactivated/dead-and-disabled enemies are automatically excluded --
        // no need to separately check activeInHierarchy per result.
        Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

        int count = 0;
        for (int i = 0; i < allColliders.Length; i++)
        {
            if (((1 << allColliders[i].gameObject.layer) & enemyLayer.value) != 0)
                count++;
        }
        return count;
    }

    private void TriggerWin()
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
        // playthrough, since this now only ever runs after we've confirmed
        // zero enemies remain on the watched layer.
        if (starRatingManager != null && rating != null)
            starRatingManager.EvaluateScore(rating.OxygenDeliver, rating.EnemyKilled);
    }

    /// <summary>
    /// Public in case you'd rather trigger the win manually from elsewhere
    /// (a different event, a test button, etc.) instead of the layer scan.
    /// </summary>
    public void ForceWin()
    {
        TriggerWin();
    }
}