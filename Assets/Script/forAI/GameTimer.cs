using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text timerText;

    [Header("Timer Settings")]
    public float missionTime = 60f;

    [Header("Results Screen")]
    public GameObject playerUI;           // The player HUD to hide
    public GameObject resultsScreen;      // The results panel to show
    public StarRatingManager starRatingManager;
    public AI_TestTD aiTestTD;            // Your script with comptTime and performanceScore

    [Header("On Timer End (Optional)")]
    [Tooltip("Optional. If assigned, this GameObject will be deactivated when the timer reaches 0.")]
    public GameObject objectToDeactivateOnTimerEnd;
    [Tooltip("Optional. If assigned, this GameObject will be reactivated when the timer reaches 0.")]
    public GameObject objectToReactivateOnTimerEnd;
    [Tooltip("Optional. Any methods hooked up here will run when the timer reaches 0. " +
             "Drag in any GameObject with a script, then pick a public method/function to call.")]
    public UnityEvent onTimerEnd;
    [Tooltip("Optional. If assigned, this GameObject will be activated when the timer reaches 0.")]
    public GameObject objectToActivateOnTimerEnd;

    [Header("ICE Bar Value -> ValuesForStar (Optional)")]
    [Tooltip("If enabled, iceBarSlider's current value is recorded into valuesForStar's ICE " +
             "field (BarValue) once, the moment this timer reaches 0. Leave unchecked to skip " +
             "this entirely - this timer doesn't have to be the one tied to ICE.")]
    public bool enableIceBarValueRecord = false;
    [Tooltip("The ICE UI slider to read from. Only used if enableIceBarValueRecord is checked.")]
    public Slider iceBarSlider;
    [Tooltip("ValuesForStar component to report into. Only used if enableIceBarValueRecord is checked.")]
    public ValuesForStar valuesForStar;

    private float currentTime;
    public bool timerActive = false;
    private bool resultsShown = false;    // Prevent triggering more than once

    void Start()
    {
        currentTime = missionTime;
        UpdateTimerUI();

        // Make sure results screen is hidden at start
        if (resultsScreen != null)
            resultsScreen.SetActive(false);
    }

    void Update()
    {
        if (!timerActive) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            timerActive = false;
            TriggerResults();
        }

        UpdateTimerUI();
    }

    // =========================
    // 🏆 RESULTS TRIGGER
    // =========================
    private void TriggerResults()
    {
        if (resultsShown) return;
        resultsShown = true;

        // Hide player UI
        if (playerUI != null)
            playerUI.SetActive(false);

        // Show results screen
        if (resultsScreen != null)
            resultsScreen.SetActive(true);

        // Evaluate and display star rating
        if (starRatingManager != null && aiTestTD != null)
            starRatingManager.EvaluateScore(aiTestTD.comptTime, aiTestTD.performanceScore, aiTestTD.idleTime, aiTestTD.FailedDelivery);

        // Optional: deactivate/reactivate additional objects when the timer ends.
        // Both fields are optional — leave either (or both) unassigned in the Inspector to skip.
        if (objectToDeactivateOnTimerEnd != null)
            objectToDeactivateOnTimerEnd.SetActive(false);

        if (objectToReactivateOnTimerEnd != null)
            objectToReactivateOnTimerEnd.SetActive(true);

        // Optional: run any custom script method(s) hooked up in the Inspector.
        // Safe to leave empty — Invoke() on an empty UnityEvent does nothing.
        onTimerEnd?.Invoke();

        // Optional: activate an additional GameObject when the timer ends.
        // Leave unassigned in the Inspector to skip.
        if (objectToActivateOnTimerEnd != null)
            objectToActivateOnTimerEnd.SetActive(true);

        // Optional: record the ICE slider's value into ValuesForStar. Gated by
        // enableIceBarValueRecord so timers not tied to ICE can just leave it off.
        RecordIceBarValue();
    }

    // Records iceBarSlider's current value into valuesForStar's BarValue field.
    // Only runs if enableIceBarValueRecord is checked and both references are
    // assigned. Called once from TriggerResults(), which is itself already
    // guarded by resultsShown - so this can never fire twice per attempt.
    private void RecordIceBarValue()
    {
        if (!enableIceBarValueRecord) return;

        if (iceBarSlider == null || valuesForStar == null)
        {
            Debug.LogWarning($"[GameTimer] {name}: enableIceBarValueRecord is checked but " +
                "iceBarSlider and/or valuesForStar isn't assigned - skipping the ICE record.");
            return;
        }

        valuesForStar.ReportBarValue(iceBarSlider.value);
        Debug.Log($"[GameTimer] {name}: timer reached 0 — recorded ICE slider value " +
            $"({iceBarSlider.value}) to ValuesForStar.BarValue.");
    }

    // =========================
    // ⏱ UI DISPLAY
    // =========================
    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    // =========================
    // 🔓 ACCESS FOR AI SYSTEM
    // =========================
    public float GetCurrentTime()
    {
        return currentTime;
    }

    // =========================
    // ▶ CONTROL
    // =========================
    public void ActivateTimer()
    {
        timerActive = true;
        currentTime = missionTime;
        resultsShown = false;
        UpdateTimerUI();
    }

    public void StopTimer()
    {
        timerActive = false;
    }
    public void ResumeTimer()
    {
        timerActive = true;
    }
    public void ResetTimer()
    {
        currentTime = missionTime;
        resultsShown = false;
        UpdateTimerUI();
    }
}