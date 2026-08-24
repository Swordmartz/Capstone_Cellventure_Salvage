using UnityEngine;

/// <summary>
/// Watches RBCTracker and triggers a win once the infection percentage
/// drops at or below a target threshold. Attach this anywhere in the scene
/// (it will find/use RBCTracker.Instance automatically).
/// </summary>
public class MalariaWinCondition : MonoBehaviour
{
    [Header("Win Threshold")]
    [Tooltip("Win when infected RBC percentage is at or below this value. 0.05 = 5%.")]
    [Range(0f, 1f)]
    [SerializeField] private float winInfectionThreshold = 0.05f;
    public StarRatingManager starRatingManager;
    public ValuesForStar rating;

    [Tooltip("If true, requires at least one RBC to exist before checking win " +
             "so the game can't 'win' before any RBCs have spawned in.")]
    [SerializeField] private bool requireRBCsToExist = true;

    [Tooltip("If true, requires infection to have actually occurred at least once before " +
             "checking win - otherwise a freshly-started level with 0% infection (because " +
             "nothing has been infected yet) would satisfy the threshold instantly.")]
    [SerializeField] private bool requireInfectionToHaveOccurred = true;

    [Header("Win Objects")]
    [Tooltip("The GameObject to deactivate when the win condition is met (e.g. gameplay UI/HUD).")]
    [SerializeField] private GameObject objectToDeactivate;

    [Tooltip("The GameObject to reactivate when the win condition is met (e.g. a win screen panel).")]
    [SerializeField] private GameObject objectToReactivate;

    private bool hasWon = false;

    // Latches to true the first time we observe InfectedRBC > 0, so the win
    // check can tell "infection was cured down to threshold" apart from
    // "infection simply hasn't started yet".
    private bool infectionHasOccurred = false;

    private void OnEnable()
    {
        if (RBCTracker.Instance != null)
            RBCTracker.Instance.OnCountsChanged += CheckWinCondition;
    }

    private void OnDisable()
    {
        if (RBCTracker.Instance != null)
            RBCTracker.Instance.OnCountsChanged -= CheckWinCondition;
    }

    private void Start()
    {
        // In case RBCTracker.Instance wasn't ready yet during OnEnable (script execution order),
        // subscribe again here and do an initial check.
        if (RBCTracker.Instance != null)
        {
            RBCTracker.Instance.OnCountsChanged -= CheckWinCondition; // avoid double-subscribe
            RBCTracker.Instance.OnCountsChanged += CheckWinCondition;
            CheckWinCondition();
        }
        else
        {
            Debug.LogWarning("MalariaWinCondition: No RBCTracker found in scene yet.");
        }
    }

    private void CheckWinCondition()
    {
        if (hasWon) return;

        var tracker = RBCTracker.Instance;
        if (tracker == null) return;

        if (requireRBCsToExist && tracker.TotalRBC <= 0)
            return;

        // Latch once infection has actually happened at least once.
        if (tracker.InfectedRBC > 0)
            infectionHasOccurred = true;

        if (requireInfectionToHaveOccurred && !infectionHasOccurred)
            return;

        if (tracker.InfectionPercentage <= winInfectionThreshold)
        {
            hasWon = true;
            Debug.Log($"Win condition met: infection at {tracker.InfectionPercentage:P1} " +
                      $"(threshold {winInfectionThreshold:P1})");
            TriggerWinObject();
        }
    }

    private void TriggerWinObject()
    {
        if (hasWon)
            return;

        hasWon = true;

        // Hide player UI
        if (objectToDeactivate != null)
            objectToDeactivate.SetActive(false);

        // Show results screen
        if (objectToReactivate != null)
            objectToReactivate.SetActive(true);

        // Evaluate and display star rating — this reads whatever RBC/WBC/ICE
        // values ValuesForStar has accumulated from ReportOxygenDeliver /
        // ReportEnemyKilled / ReportBarValue over the course of the actual
        // playthrough, since this now only ever runs after we've confirmed
        // zero enemies remain on the watched layer.
        if (starRatingManager != null && rating != null)
            starRatingManager.EvaluateScore(rating.OxygenDeliver, rating.EnemyKilled);
    }

    /// <summary>Call this if you need to reset the win state (e.g. restarting a level).</summary>
    public void ResetWinState()
    {
        hasWon = false;
        infectionHasOccurred = false;
    }
}