using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StarRatingManager : MonoBehaviour
{
    [Header("Stars (assign 3 in order)")]
    public Image[] starImages;

    [Header("Star 1 Sprites")]
    public Sprite star1Filled;
    public Sprite star1Empty;

    [Header("Star 2 Sprites")]
    public Sprite star2Filled;
    public Sprite star2Empty;

    [Header("Star 3 Sprites")]
    public Sprite star3Filled;
    public Sprite star3Empty;

    [Header("UI Text")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI feedbackText;

    [Header("Animation Settings")]
    public float starMaxScale = 8f;
    public float animDuration = 0.6f;
    public float delayBetweenStars = 0.3f;

    [Header("Level Settings")]
    public float maxTime = 120f;

    [Header("Scoring Weights (must add up to 1)")]
    [Tooltip("Weight for completion time (faster = better).")]
    [Range(0f, 1f)] public float timeWeight = 0.35f;
    [Tooltip("Weight for performance score.")]
    [Range(0f, 1f)] public float performanceWeight = 0.35f;
    [Tooltip("Weight for idle time penalty (less idle = better).")]
    [Range(0f, 1f)] public float idleWeight = 0.15f;
    [Tooltip("Weight for failed delivery penalty.")]
    [Range(0f, 1f)] public float failedDeliveryWeight = 0.15f;

    [Header("Penalty Settings")]
    [Tooltip("Each idle second reduces idle score by this much.")]
    public float idlePenaltyPerSecond = 0.05f;
    [Tooltip("Each failed delivery reduces that score by this much.")]
    public float failedDeliveryPenalty = 0.2f;

    [Header("Star Thresholds")]
    [Tooltip("Final score (0-1) needed for 3 stars.")]
    [Range(0f, 1f)] public float threshold3Stars = 0.80f;
    [Tooltip("Final score (0-1) needed for 2 stars.")]
    [Range(0f, 1f)] public float threshold2Stars = 0.50f;
    [Tooltip("Final score (0-1) needed for 1 star.")]
    [Range(0f, 1f)] public float threshold1Star = 0.20f;

    // ─────────────────────────────────────────────────────────────────────────

    private Sprite GetFilled(int index)
    {
        switch (index)
        {
            case 0: return star1Filled;
            case 1: return star2Filled;
            case 2: return star3Filled;
            default: return null;
        }
    }

    private Sprite GetEmpty(int index)
    {
        switch (index)
        {
            case 0: return star1Empty;
            case 1: return star2Empty;
            case 2: return star3Empty;
            default: return null;
        }
    }

    /// <summary>
    /// Call this when the level ends.
    /// Pass the four values straight from AI_TestTD.
    /// </summary>
    public void EvaluateFromMission(AI_TestTD missionData)
    {
        EvaluateScore(
            missionData.comptTime,
            missionData.performanceScore,
            missionData.idleTime,
            missionData.FailedDelivery
        );
    }

    /// <summary>
    /// Core evaluation — can also be called directly with raw values.
    /// </summary>
    public void EvaluateScore(float completionTime, float performanceScore,
                               int idleTime, int failedDeliveries)
    {
        StopAllCoroutines();
        StartCoroutine(EvaluateRoutine(completionTime, performanceScore,
                                       idleTime, failedDeliveries));
    }

    private IEnumerator EvaluateRoutine(float completionTime, float performanceScore,
                                         int idleTime, int failedDeliveries)
    {
        yield return null;

        // ── Time score: 1 if completed instantly, 0 if used all maxTime ──────
        float timeScore = 1f - Mathf.Clamp01(completionTime / maxTime);

        // ── Performance score: normalise 0-5 range from AI_TestTD heuristic ──
        // performanceScore is accumulated across 3 checkpoints, max ~15 (3 × 5)
        float perfScore = Mathf.Clamp01(performanceScore / 15f);

        // ── Idle penalty: more idle = lower score ─────────────────────────────
        float idleScore = Mathf.Clamp01(1f - idleTime * idlePenaltyPerSecond);

        // ── Failed delivery penalty ───────────────────────────────────────────
        float deliveryScore = Mathf.Clamp01(1f - failedDeliveries * failedDeliveryPenalty);

        // ── Weighted final score ──────────────────────────────────────────────
        float finalScore = (timeScore * timeWeight)
                         + (perfScore * performanceWeight)
                         + (idleScore * idleWeight)
                         + (deliveryScore * failedDeliveryWeight);

        finalScore = Mathf.Clamp01(finalScore);

        int stars = GetStars(finalScore);

        Debug.Log($"[StarRating] Time:{completionTime} | Perf:{performanceScore} " +
                  $"| Idle:{idleTime} | Failed:{failedDeliveries} " +
                  $"| FinalScore:{finalScore:F2} | Stars:{stars}");

        // ── Update UI ─────────────────────────────────────────────────────────
        scoreText.text = "Score: " + Mathf.RoundToInt(finalScore * 100);
        timeText.text = "Time: " + FormatTime(completionTime);
        feedbackText.text = GetFeedback(stars);

        for (int i = 0; i < starImages.Length; i++)
        {
            starImages[i].sprite = GetEmpty(i);
            starImages[i].transform.localScale = Vector3.one;
        }

        yield return StartCoroutine(AnimateStars(stars));
    }

    private int GetStars(float score)
    {
        if (score >= threshold3Stars) return 3;
        if (score >= threshold2Stars) return 2;
        if (score >= threshold1Star) return 1;
        return 0;
    }

    private IEnumerator AnimateStars(int earnedStars)
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            yield return new WaitForSeconds(delayBetweenStars);

            if (i < earnedStars)
            {
                starImages[i].sprite = GetFilled(i);
                yield return StartCoroutine(AnimateStar(starImages[i].transform));
            }
        }
    }

    private IEnumerator AnimateStar(Transform starTransform)
    {
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float scale = Mathf.Lerp(starMaxScale, 1f, EaseOutBack(t));
            starTransform.localScale = Vector3.one * scale;
            yield return null;
        }
        starTransform.localScale = Vector3.one;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0:00}:{1:00}", mins, secs);
    }

    private string GetFeedback(int stars)
    {
        switch (stars)
        {
            case 3: return "Excellent!";
            case 2: return "Good Job!";
            case 1: return "Keep Trying!";
            default: return "Try Again!";
        }
    }
}