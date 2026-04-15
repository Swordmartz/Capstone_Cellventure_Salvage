using UnityEngine;

/// <summary>
/// Fuzzy Logic evaluator.
/// - Completion time: HIGHER = BETTER (survived longer), max = missionTime
/// - Performance score: HIGHER = BETTER, max 15 points
///
/// Star thresholds (based on fuzzy score 0-100):
/// 3 stars = 75% and above  (75-100)
/// 2 stars = 30% to 74%     (30-74)
/// 1 star  = 1% to 29%      (1-29)
/// 0 stars = 0%              (0)
/// </summary>
public static class FuzzyLogicEvaluator
{
    // ── Fuzzy Membership Functions ───────────────────────────────────────────

    // HIGH membership: value is close to 1 (best)
    private static float MemberHigh(float norm)
    {
        return Mathf.Clamp01((norm - 0.5f) / 0.5f);   // ramps up from 0.5 to 1.0
    }

    // MEDIUM membership: value is around the middle
    private static float MemberMedium(float norm)
    {
        return Mathf.Clamp01(1f - Mathf.Abs(norm - 0.5f) / 0.5f); // peaks at 0.5
    }

    // LOW membership: value is close to 0 (worst)
    private static float MemberLow(float norm)
    {
        return Mathf.Clamp01(1f - (norm / 0.5f));      // ramps down from 0 to 0.5
    }

    // ── Evaluate ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns fuzzy score 0-100.
    /// completionTime / maxTime = time ratio (higher = better)
    /// performanceScore / 15   = perf ratio  (higher = better)
    /// </summary>
    public static float Evaluate(float completionTime, float maxTime, float performanceScore)
    {
        float normTime = Mathf.Clamp01(completionTime / maxTime);
        float normPerf = Mathf.Clamp01(performanceScore / 15f);

        // Time memberships
        float timeHigh = MemberHigh(normTime);
        float timeMedium = MemberMedium(normTime);
        float timeLow = MemberLow(normTime);

        // Performance memberships
        float perfHigh = MemberHigh(normPerf);
        float perfMedium = MemberMedium(normPerf);
        float perfLow = MemberLow(normPerf);

        // ── Fuzzy Rules ──────────────────────────────────────────────────────
        // Format: IF time is X AND perf is Y THEN score is Z
        // Rule strength = MIN(antecedents) * output value

        float rule1 = Mathf.Min(timeHigh, perfHigh) * 100f;  // high time + high perf   = 100
        float rule2 = Mathf.Min(timeHigh, perfMedium) * 80f;   // high time + medium perf  = 80
        float rule3 = Mathf.Min(timeHigh, perfLow) * 50f;   // high time + low perf     = 50
        float rule4 = Mathf.Min(timeMedium, perfHigh) * 80f;   // medium time + high perf  = 80
        float rule5 = Mathf.Min(timeMedium, perfMedium) * 55f;   // medium time + medium perf= 55
        float rule6 = Mathf.Min(timeMedium, perfLow) * 30f;   // medium time + low perf   = 30
        float rule7 = Mathf.Min(timeLow, perfHigh) * 50f;   // low time + high perf     = 50
        float rule8 = Mathf.Min(timeLow, perfMedium) * 25f;   // low time + medium perf   = 25
        float rule9 = Mathf.Min(timeLow, perfLow) * 1f;    // low time + low perf      = 1

        // Defuzzify using weighted average
        float totalStrength = timeHigh + timeMedium + timeLow + perfHigh + perfMedium + perfLow;

        if (totalStrength < Mathf.Epsilon) return 0f;

        float fuzzyScore = (rule1 + rule2 + rule3 + rule4 + rule5 + rule6 + rule7 + rule8 + rule9)
                         / totalStrength;

        Debug.Log($"[FuzzyEval] normTime:{normTime:F2} normPerf:{normPerf:F2} | " +
                  $"timeH:{timeHigh:F2} timeM:{timeMedium:F2} timeL:{timeLow:F2} | " +
                  $"perfH:{perfHigh:F2} perfM:{perfMedium:F2} perfL:{perfLow:F2} | " +
                  $"FuzzyScore:{fuzzyScore:F1}");

        return Mathf.Clamp(fuzzyScore, 0f, 100f);
    }

    // ── Stars from Fuzzy Score ────────────────────────────────────────────────

    public static int GetStars(float fuzzyScore)
    {
        if (fuzzyScore >= 75f) return 3;   // 75% - 100%
        if (fuzzyScore >= 30f) return 2;   // 30% - 74%
        if (fuzzyScore >= 1f) return 1;   // 1%  - 29%
        return 0;                           // 0%
    }
}