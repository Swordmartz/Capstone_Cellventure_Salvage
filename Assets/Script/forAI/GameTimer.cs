using UnityEngine;
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
            starRatingManager.EvaluateScore(aiTestTD.comptTime, aiTestTD.performanceScore);
        

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

    public void ResetTimer()
    {
        currentTime = missionTime;
        resultsShown = false;
        UpdateTimerUI();
    }
}