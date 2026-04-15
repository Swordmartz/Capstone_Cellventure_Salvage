using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StarRatingManager : MonoBehaviour
{
    [Header("Stars (assign 3 in order)")]
    public Image[] starImages;              // 3 star Image components

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

    // Helper to get the correct filled/empty sprite per star index
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

    public void EvaluateScore(float completionTime, float performanceScore)
    {
        StopAllCoroutines();
        StartCoroutine(EvaluateRoutine(completionTime, performanceScore));
    }

    private IEnumerator EvaluateRoutine(float completionTime, float performanceScore)
    {
        yield return null;

        float fuzzyScore = FuzzyLogicEvaluator.Evaluate(completionTime, maxTime, performanceScore);
        int stars = FuzzyLogicEvaluator.GetStars(fuzzyScore);

        Debug.Log($"[StarRating] CompletionTime:{completionTime} | MaxTime:{maxTime} | PerfScore:{performanceScore} | FuzzyScore:{fuzzyScore} | Stars:{stars}");

        scoreText.text = "Score: " + performanceScore.ToString();
        timeText.text = "Time: " + FormatTime(completionTime);
        feedbackText.text = GetFeedback(stars);

        // Reset all stars to empty using their individual sprites
        for (int i = 0; i < starImages.Length; i++)
        {
            starImages[i].sprite = GetEmpty(i);
            starImages[i].transform.localScale = Vector3.one;
        }

        yield return StartCoroutine(AnimateStars(stars));
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