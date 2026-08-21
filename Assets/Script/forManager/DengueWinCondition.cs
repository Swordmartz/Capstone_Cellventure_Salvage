using UnityEngine;

/// <summary>
/// Win condition for the Dengue level -- watches InflammationManager and
/// triggers once NormalizedInflammation drops to (or below) a low threshold,
/// rather than tracking a single enemy's death state or an enemy layer
/// being empty.
///
/// Same "don't win instantly" guard as the other win conditions: inflammation
/// starts at 0 before any leaks/enemies/infected cells have registered
/// themselves, so this waits until inflammation has actually risen above the
/// threshold at least once before it's allowed to fire -- otherwise it would
/// trigger the moment the scene loads, before the player has done anything.
/// </summary>
public class DengueWinCondition : MonoBehaviour
{
    public StarRatingManager starRatingManager;
    public ValuesForStar rating;

    [Header("Inflammation Threshold")]
    [Tooltip("Win triggers once InflammationManager.NormalizedInflammation drops to or below this value (0-1 scale). E.g. 0.1 = must be down to 10% inflamed or less.")]
    [SerializeField, Range(0f, 1f)] private float lowInflammationThreshold = 0.1f;

    [Tooltip("If true, waits until inflammation has risen above the threshold at least once before it will ever declare a win -- prevents an instant win on scene load, before anything has raised inflammation yet.")]
    [SerializeField] private bool waitForInflammationToRiseFirst = true;

    [Header("Win UI")]
    [Tooltip("Gameplay UI to disable once the win condition is met (e.g. health bars, HUD).")]
    [SerializeField] private GameObject uiToDisable;

    [Tooltip("Win screen (or whatever else) to enable once the win condition is met.")]
    [SerializeField] private GameObject winScreen;

    private bool hasRisenAboveThreshold;
    private bool winTriggered;

    private void Update()
    {
        if (winTriggered)
            return;



        if (InflammationManager.Instance == null)
            return;

        float inflammation = InflammationManager.Instance.NormalizedInflammation;

        if (inflammation > lowInflammationThreshold)
        {
            hasRisenAboveThreshold = true;
            return;
        }

        // inflammation is at/below the threshold from here on
        if (waitForInflammationToRiseFirst && !hasRisenAboveThreshold)
            return; // never actually got inflamed yet -- don't win by default

        TriggerWin();
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
            starRatingManager.EvaluateScore(rating.OxygenDeliver, rating.EnemyKilled, rating.WoundHealed);
    }

    /// <summary>
    /// Public in case you'd rather trigger the win manually from elsewhere
    /// (a different event, a test button, etc.) instead of the threshold check.
    /// </summary>
    public void ForceWin()
    {
        TriggerWin();
    }
}