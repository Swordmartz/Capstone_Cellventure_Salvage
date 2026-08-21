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

    [Header("Formula Selection")]
    public bool useFormula1 = true;
    [Tooltip("Formula - Ascariasis: computes the score from ValuesForStar's RBC (OxygenDeliver), " +
             "WBC (EnemyKilled), and ICE (BarValue) fields instead of the time/performance/idle/" +
             "delivery params passed into EvaluateScore.")]
    public bool useFormulaAscariasis = false;
    [Tooltip("Formula - Influenza: computes the score from ValuesForStar's RBC (OxygenDeliver) and " +
             "WBC (EnemyKilled) fields only (no ICE component).")]
    public bool useFormulaInfluenza = false;
    [Tooltip("Formula - Pneumococcal: same RBC + WBC shape as Influenza (OxygenDeliver + EnemyKilled, " +
             "no ICE component), but with its own max values, weights, and star thresholds so a " +
             "different scene/level can be tuned independently.")]
    public bool useFormulaPneumococcal = false;
    [Tooltip("Formula - Dengue: computes the score from ValuesForStar's RBC (OxygenDeliver), " +
             "WBC (EnemyKilled), and Platelets (WoundHealed) fields (no ICE component).")]
    public bool useFormulaDengue = false;
    // public bool useFormula2 = false;  // uncomment when ready
    // public bool useFormula3 = false;

    [Header("Formula 1 — Time + Performance + Idle + Delivery")]
    public float timeWeight = 0.35f;
    public float performanceWeight = 0.35f;
    public float idleWeight = 0.15f;
    public float failedDeliveryWeight = 0.15f;
    public float idlePenaltyPerSecond = 0.05f;
    public float failedDeliveryPenalty = 0.2f;

    [Header("Formula - Ascariasis — RBC + WBC + ICE")]
    [Tooltip("ValuesForStar component this formula reads OxygenDeliver, EnemyKilled, and " +
             "BarValue from. Required if useFormulaAscariasis is checked.")]
    public ValuesForStar valuesForStar;

    [Tooltip("OxygenDeliver value that counts as a full (1.0) RBC score. Actual value is " +
             "divided by this and clamped 0-1.")]
    public float rbcMaxOxygenDeliver = 3f;
    [Tooltip("EnemyKilled count that counts as a full (1.0) WBC score. Actual value is " +
             "divided by this and clamped 0-1.")]
    public int wbcMaxEnemyKilled = 1;
    [Tooltip("BarValue that counts as a full (1.0) ICE score. Actual value is divided by " +
             "this and clamped 0-1.")]
    public float iceMaxBarValue = 60f;

    [Tooltip("How much each of the three scores above contributes to the final Ascariasis score. " +
             "These three should add up to 1 for the final score to land cleanly in the 0-1 range.")]
    public float rbcWeight = 0.34f;
    public float wbcWeight = 0.33f;
    public float iceWeight = 0.33f;

    [Header("Formula - Influenza — RBC + WBC")]
    [Tooltip("OxygenDeliver value that counts as a full (1.0) RBC score for this formula. Actual " +
             "value is divided by this and clamped 0-1. Kept separate from rbcMaxOxygenDeliver above " +
             "in case Influenza levels need a different max.")]
    public float rbcMaxOxygenDeliverInfluenza = 3f;
    [Tooltip("EnemyKilled count that counts as a full (1.0) WBC score for this formula. Actual value " +
             "is divided by this and clamped 0-1. Kept separate from wbcMaxEnemyKilled above in case " +
             "Influenza levels need a different max.")]
    public int wbcMaxEnemyKilledInfluenza = 1;

    [Tooltip("How much each of the two scores above contributes to the final Influenza score. " +
             "These two should add up to 1 for the final score to land cleanly in the 0-1 range.")]
    public float rbcWeightInfluenza = 0.5f;
    public float wbcWeightInfluenza = 0.5f;

    [Header("Formula - Pneumococcal — RBC + WBC")]
    [Tooltip("OxygenDeliver value that counts as a full (1.0) RBC score for this formula. Actual " +
             "value is divided by this and clamped 0-1. Kept separate from the other formulas' maxes " +
             "so this scene can be tuned independently.")]
    public float rbcMaxOxygenDeliverPneumococcal = 3f;
    [Tooltip("EnemyKilled count that counts as a full (1.0) WBC score for this formula. Actual value " +
             "is divided by this and clamped 0-1. Kept separate from the other formulas' maxes so this " +
             "scene can be tuned independently.")]
    public int wbcMaxEnemyKilledPneumococcal = 1;

    [Tooltip("How much each of the two scores above contributes to the final Pneumococcal score. " +
             "These two should add up to 1 for the final score to land cleanly in the 0-1 range.")]
    public float rbcWeightPneumococcal = 0.5f;
    public float wbcWeightPneumococcal = 0.5f;

    [Header("Formula - Dengue — RBC + WBC + Platelets")]
    [Tooltip("OxygenDeliver value that counts as a full (1.0) RBC score for this formula. Actual " +
             "value is divided by this and clamped 0-1. Kept separate from the other formulas' maxes " +
             "so this scene can be tuned independently.")]
    public float rbcMaxOxygenDeliverDengue = 3f;
    [Tooltip("EnemyKilled count that counts as a full (1.0) WBC score for this formula. Actual value " +
             "is divided by this and clamped 0-1. Kept separate from the other formulas' maxes so this " +
             "scene can be tuned independently.")]
    public int wbcMaxEnemyKilledDengue = 1;
    [Tooltip("WoundHealed count that counts as a full (1.0) Platelets score for this formula. Actual " +
             "value is divided by this and clamped 0-1.")]
    public int plateletsMaxWoundHealedDengue = 1;

    [Tooltip("How much each of the three scores above contributes to the final Dengue score. " +
             "These three should add up to 1 for the final score to land cleanly in the 0-1 range.")]
    public float rbcWeightDengue = 0.34f;
    public float wbcWeightDengue = 0.33f;
    public float plateletsWeightDengue = 0.33f;

    // [Header("Formula 2 — ...")]  // add variables here when ready

    [Header("Star Thresholds — Formula 1")]
    [Range(0f, 1f)] public float threshold3StarsFormula1 = 0.80f;
    [Range(0f, 1f)] public float threshold2StarsFormula1 = 0.50f;
    [Range(0f, 1f)] public float threshold1StarFormula1 = 0.20f;

    [Header("Star Thresholds — Ascariasis")]
    [Range(0f, 1f)] public float threshold3StarsAscariasis = 0.80f;
    [Range(0f, 1f)] public float threshold2StarsAscariasis = 0.50f;
    [Range(0f, 1f)] public float threshold1StarAscariasis = 0.20f;

    [Header("Star Thresholds — Influenza")]
    [Range(0f, 1f)] public float threshold3StarsInfluenza = 0.80f;
    [Range(0f, 1f)] public float threshold2StarsInfluenza = 0.50f;
    [Range(0f, 1f)] public float threshold1StarInfluenza = 0.20f;

    [Header("Star Thresholds — Pneumococcal")]
    [Range(0f, 1f)] public float threshold3StarsPneumococcal = 0.80f;
    [Range(0f, 1f)] public float threshold2StarsPneumococcal = 0.50f;
    [Range(0f, 1f)] public float threshold1StarPneumococcal = 0.20f;

    [Header("Star Thresholds — Dengue")]
    [Range(0f, 1f)] public float threshold3StarsDengue = 0.80f;
    [Range(0f, 1f)] public float threshold2StarsDengue = 0.50f;
    [Range(0f, 1f)] public float threshold1StarDengue = 0.20f;

    /// <summary>
    /// Which formula produced a given score, so GetStars() knows which
    /// threshold set to compare against.
    /// </summary>
    private enum FormulaType
    {
        Formula1,
        Ascariasis,
        Influenza,
        Pneumococcal,
        Dengue
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void OnValidate()
    {
        // Just warn — don't silently flip anything. EvaluateRoutine below refuses to run
        // if more than one (or zero) formula booleans are checked, so you'll see this
        // warning AND a hard error in Console rather than a wrong score sneaking through.
        int checkedCount = (useFormula1 ? 1 : 0) + (useFormulaAscariasis ? 1 : 0)
            + (useFormulaInfluenza ? 1 : 0) + (useFormulaPneumococcal ? 1 : 0) + (useFormulaDengue ? 1 : 0);
        if (checkedCount > 1)
        {
            Debug.LogWarning("[StarRating] More than one formula boolean is checked " +
                "(useFormula1 / useFormulaAscariasis / useFormulaInfluenza / useFormulaPneumococcal / " +
                "useFormulaDengue). Only ONE should be checked at a time. EvaluateScore will refuse to " +
                "compute a score until you fix this.");
        }
        else if (checkedCount == 0)
        {
            Debug.LogWarning("[StarRating] No formula boolean is checked. " +
                "EvaluateScore will refuse to compute a score until you check exactly one.");
        }
    }

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

    public void EvaluateFromMission(AI_TestTD missionData)
    {
        Debug.Log("[StarRating] EvaluateFromMission called");
        EvaluateScore(
            missionData.comptTime,
            missionData.performanceScore,
            missionData.idleTime,
            missionData.FailedDelivery
        );
    }

    public void EvaluateScore(float completionTime, float performanceScore,
                               int idleTime, int failedDeliveries)
    {
        Debug.Log($"[StarRating] EvaluateScore called (gameObject active: {gameObject.activeInHierarchy}) " +
                   $"- time={completionTime}, perf={performanceScore}, idle={idleTime}, failed={failedDeliveries}");

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[StarRating] This GameObject is INACTIVE — the coroutine below will " +
                "not run (or will fail to start) until it's active. Enable the object BEFORE calling EvaluateScore.");
        }

        StopAllCoroutines();
        StartCoroutine(EvaluateRoutine(completionTime, performanceScore,
                                       idleTime, failedDeliveries));
    }

    /// <summary>
    /// Overload for the two RBC + WBC + (third value) formulas (Ascariasis and
    /// Dengue) that takes the three values directly instead of reading them
    /// from a ValuesForStar reference. Use this when you already have the raw
    /// values in hand and don't want to route them through that component first.
    ///
    /// Both formulas share this exact same signature (RBC, WBC, and a third
    /// value - ICE for Ascariasis, Platelets for Dengue), so this method
    /// routes between them based on which of useFormulaAscariasis /
    /// useFormulaDengue is checked on THIS scene's StarRatingManager instance -
    /// same pattern as the 2-param EvaluateScore(rbc, wbc) below routing
    /// between Influenza and Pneumococcal.
    /// </summary>
    public void EvaluateScore(float rbcValue, float wbcValue, float thirdValue)
    {
        Debug.Log($"[StarRating] EvaluateScore (RBC+WBC+3rd 3-param) called " +
                   $"(gameObject active: {gameObject.activeInHierarchy}) " +
                   $"- rbc={rbcValue}, wbc={wbcValue}, third={thirdValue}");

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[StarRating] This GameObject is INACTIVE — the coroutine below will " +
                "not run (or will fail to start) until it's active. Enable the object BEFORE calling EvaluateScore.");
        }

        // Validate exactly one of the two RBC+WBC+3rd formulas is selected
        // before picking a routine, same principle as EvaluateRoutine's own guard.
        int checkedCount = (useFormulaAscariasis ? 1 : 0) + (useFormulaDengue ? 1 : 0);
        if (checkedCount == 0)
        {
            Debug.LogError("[StarRating] EvaluateScore(rbc, wbc, thirdValue) was called but neither " +
                "useFormulaAscariasis nor useFormulaDengue is checked - check exactly one on this " +
                "StarRatingManager to say which formula this scene should use.");
            return;
        }
        if (checkedCount > 1)
        {
            Debug.LogError("[StarRating] EvaluateScore(rbc, wbc, thirdValue) was called but BOTH " +
                "useFormulaAscariasis and useFormulaDengue are checked - uncheck all but the one " +
                "this scene should use, then try again.");
            return;
        }

        StopAllCoroutines();

        if (useFormulaAscariasis)
            StartCoroutine(EvaluateAscariasisRoutine(rbcValue, wbcValue, thirdValue));
        else
            StartCoroutine(EvaluateDengueRoutine(rbcValue, wbcValue, thirdValue));
    }

    /// <summary>
    /// Overload for the two RBC + WBC-only formulas (Influenza and Pneumococcal)
    /// that takes the two values directly instead of reading them from a
    /// ValuesForStar reference. Use this when you already have the raw values
    /// in hand and don't want to route them through that component first.
    ///
    /// Both formulas share this exact same signature, so this method routes
    /// between them based on which of useFormulaInfluenza / useFormulaPneumococcal
    /// is checked on THIS scene's StarRatingManager instance - that's what lets
    /// different scenes call the same EvaluateScore(rbc, wbc) but each get their
    /// own formula, weights, and star thresholds.
    /// </summary>
    public void EvaluateScore(float rbcValue, float wbcValue)
    {
        Debug.Log($"[StarRating] EvaluateScore (RBC+WBC 2-param) called " +
                   $"(gameObject active: {gameObject.activeInHierarchy}) " +
                   $"- rbc={rbcValue}, wbc={wbcValue}");

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[StarRating] This GameObject is INACTIVE — the coroutine below will " +
                "not run (or will fail to start) until it's active. Enable the object BEFORE calling EvaluateScore.");
        }

        // Validate exactly one of the two RBC+WBC formulas is selected before
        // picking a routine, same principle as EvaluateRoutine's own guard.
        int checkedCount = (useFormulaInfluenza ? 1 : 0) + (useFormulaPneumococcal ? 1 : 0);
        if (checkedCount == 0)
        {
            Debug.LogError("[StarRating] EvaluateScore(rbc, wbc) was called but neither " +
                "useFormulaInfluenza nor useFormulaPneumococcal is checked - check exactly one " +
                "on this StarRatingManager to say which formula this scene should use.");
            return;
        }
        if (checkedCount > 1)
        {
            Debug.LogError("[StarRating] EvaluateScore(rbc, wbc) was called but BOTH " +
                "useFormulaInfluenza and useFormulaPneumococcal are checked - uncheck all but " +
                "the one this scene should use, then try again.");
            return;
        }

        StopAllCoroutines();

        if (useFormulaInfluenza)
            StartCoroutine(EvaluateInfluenzaRoutine(rbcValue, wbcValue));
        else
            StartCoroutine(EvaluatePneumococcalRoutine(rbcValue, wbcValue));
    }

    private IEnumerator EvaluateRoutine(float completionTime, float performanceScore,
                                         int idleTime, int failedDeliveries)
    {
        Debug.Log("[StarRating] Routine STARTED");
        yield return null;

        float finalScore = 0f;
        bool formulaFound = false;
        FormulaType formulaUsed = FormulaType.Formula1;

        // ── Validate exactly one formula is selected BEFORE computing anything ──
        int checkedCount = (useFormula1 ? 1 : 0) + (useFormulaAscariasis ? 1 : 0)
            + (useFormulaInfluenza ? 1 : 0) + (useFormulaPneumococcal ? 1 : 0) + (useFormulaDengue ? 1 : 0);
        if (checkedCount == 0)
        {
            Debug.LogError("[StarRating] No formula boolean is checked (useFormula1 / " +
                "useFormulaAscariasis / useFormulaInfluenza / useFormulaPneumococcal / useFormulaDengue " +
                "are all false). Check exactly one in the Inspector.");
            yield break;
        }
        if (checkedCount > 1)
        {
            Debug.LogError("[StarRating] More than one formula boolean is checked at once. " +
                "Uncheck all but the one you want to use, then try again.");
            yield break;
        }

        // ── Formula 1 ────────────────────────────────────────────────────────
        if (useFormula1)
        {
            formulaFound = true;
            formulaUsed = FormulaType.Formula1;

            float timeScore = 1f - Mathf.Clamp01(completionTime / maxTime);
            float perfScore = Mathf.Clamp01(performanceScore / 15f);
            float idleScore = Mathf.Clamp01(1f - idleTime * idlePenaltyPerSecond);
            float deliveryScore = Mathf.Clamp01(1f - failedDeliveries * failedDeliveryPenalty);

            finalScore = (timeScore * timeWeight)
                       + (perfScore * performanceWeight)
                       + (idleScore * idleWeight)
                       + (deliveryScore * failedDeliveryWeight);

            Debug.Log($"[StarRating] Formula1 -> time={timeScore}, perf={perfScore}, " +
                      $"idle={idleScore}, delivery={deliveryScore}, finalScore(pre-clamp)={finalScore}");
        }

        // ── Formula - Ascariasis (RBC + WBC + ICE) ─────────────────────────────
        else if (useFormulaAscariasis)
        {
            if (valuesForStar == null)
            {
                Debug.LogWarning("[StarRating] useFormulaAscariasis is checked but no " +
                    "valuesForStar is assigned - can't compute a score.");
                yield break;
            }

            formulaFound = true;
            formulaUsed = FormulaType.Ascariasis;

            float rbcScore = rbcMaxOxygenDeliver > 0f
                ? Mathf.Clamp01(valuesForStar.OxygenDeliver / rbcMaxOxygenDeliver)
                : 0f;

            float wbcScore = wbcMaxEnemyKilled > 0
                ? Mathf.Clamp01((float)valuesForStar.EnemyKilled / wbcMaxEnemyKilled)
                : 0f;

            float iceScore = iceMaxBarValue > 0f
                ? Mathf.Clamp01(valuesForStar.BarValue / iceMaxBarValue)
                : 0f;

            finalScore = (rbcScore * rbcWeight)
                       + (wbcScore * wbcWeight)
                       + (iceScore * iceWeight);

            Debug.Log($"[StarRating] Ascariasis -> rbc={rbcScore}, wbc={wbcScore}, " +
                      $"ice={iceScore}, finalScore(pre-clamp)={finalScore}");
        }

        // ── Formula - Influenza (RBC + WBC) ─────────────────────────────────
        else if (useFormulaInfluenza)
        {
            if (valuesForStar == null)
            {
                Debug.LogWarning("[StarRating] useFormulaInfluenza is checked but no " +
                    "valuesForStar is assigned - can't compute a score.");
                yield break;
            }

            formulaFound = true;
            formulaUsed = FormulaType.Influenza;

            float rbcScore = rbcMaxOxygenDeliverInfluenza > 0f
                ? Mathf.Clamp01(valuesForStar.OxygenDeliver / rbcMaxOxygenDeliverInfluenza)
                : 0f;

            float wbcScore = wbcMaxEnemyKilledInfluenza > 0
                ? Mathf.Clamp01((float)valuesForStar.EnemyKilled / wbcMaxEnemyKilledInfluenza)
                : 0f;

            finalScore = (rbcScore * rbcWeightInfluenza)
                       + (wbcScore * wbcWeightInfluenza);

            Debug.Log($"[StarRating] Influenza -> rbc={rbcScore}, wbc={wbcScore}, " +
                      $"finalScore(pre-clamp)={finalScore}");
        }

        // ── Formula - Pneumococcal (RBC + WBC) ──────────────────────────────
        else if (useFormulaPneumococcal)
        {
            if (valuesForStar == null)
            {
                Debug.LogWarning("[StarRating] useFormulaPneumococcal is checked but no " +
                    "valuesForStar is assigned - can't compute a score.");
                yield break;
            }

            formulaFound = true;
            formulaUsed = FormulaType.Pneumococcal;

            float rbcScore = rbcMaxOxygenDeliverPneumococcal > 0f
                ? Mathf.Clamp01(valuesForStar.OxygenDeliver / rbcMaxOxygenDeliverPneumococcal)
                : 0f;

            float wbcScore = wbcMaxEnemyKilledPneumococcal > 0
                ? Mathf.Clamp01((float)valuesForStar.EnemyKilled / wbcMaxEnemyKilledPneumococcal)
                : 0f;

            finalScore = (rbcScore * rbcWeightPneumococcal)
                       + (wbcScore * wbcWeightPneumococcal);

            Debug.Log($"[StarRating] Pneumococcal -> rbc={rbcScore}, wbc={wbcScore}, " +
                      $"finalScore(pre-clamp)={finalScore}");
        }

        // ── Formula - Dengue (RBC + WBC + Platelets) ────────────────────────
        else if (useFormulaDengue)
        {
            if (valuesForStar == null)
            {
                Debug.LogWarning("[StarRating] useFormulaDengue is checked but no " +
                    "valuesForStar is assigned - can't compute a score.");
                yield break;
            }

            formulaFound = true;
            formulaUsed = FormulaType.Dengue;

            float rbcScore = rbcMaxOxygenDeliverDengue > 0f
                ? Mathf.Clamp01(valuesForStar.OxygenDeliver / rbcMaxOxygenDeliverDengue)
                : 0f;

            float wbcScore = wbcMaxEnemyKilledDengue > 0
                ? Mathf.Clamp01((float)valuesForStar.EnemyKilled / wbcMaxEnemyKilledDengue)
                : 0f;

            float plateletsScore = plateletsMaxWoundHealedDengue > 0
                ? Mathf.Clamp01((float)valuesForStar.WoundHealed / plateletsMaxWoundHealedDengue)
                : 0f;

            finalScore = (rbcScore * rbcWeightDengue)
                       + (wbcScore * wbcWeightDengue)
                       + (plateletsScore * plateletsWeightDengue);

            Debug.Log($"[StarRating] Dengue -> rbc={rbcScore}, wbc={wbcScore}, " +
                      $"platelets={plateletsScore}, finalScore(pre-clamp)={finalScore}");
        }

        // ── Formula 2 (add when ready) ───────────────────────────────────────
        // else if (useFormula2)
        // {
        //     formulaFound = true;
        //     finalScore = ...;
        // }

        if (!formulaFound)
        {
            Debug.LogWarning("[StarRating] No formula selected!");
            yield break;
        }

        finalScore = Mathf.Clamp01(finalScore);
        int stars = GetStars(finalScore, formulaUsed);
        Debug.Log($"[StarRating] finalScore={finalScore}, stars={stars}");

        yield return StartCoroutine(FinishEvaluation(finalScore, stars, (float?)completionTime));

        Debug.Log("[StarRating] Routine FINISHED");
    }

    /// <summary>
    /// Companion to EvaluateScore(float, float, float) — computes the Ascariasis
    /// score directly from RBC/WBC/ICE values passed in, without touching
    /// valuesForStar, then reuses the same UI-update/animation path as the
    /// main EvaluateRoutine.
    /// </summary>
    private IEnumerator EvaluateAscariasisRoutine(float rbcValue, float wbcValue, float iceValue)
    {
        Debug.Log("[StarRating] Ascariasis Routine STARTED");
        yield return null;

        float rbcScore = rbcMaxOxygenDeliver > 0f
            ? Mathf.Clamp01(rbcValue / rbcMaxOxygenDeliver)
            : 0f;

        float wbcScore = wbcMaxEnemyKilled > 0
            ? Mathf.Clamp01(wbcValue / wbcMaxEnemyKilled)
            : 0f;

        float iceScore = iceMaxBarValue > 0f
            ? Mathf.Clamp01(iceValue / iceMaxBarValue)
            : 0f;

        float finalScore = (rbcScore * rbcWeight)
                          + (wbcScore * wbcWeight)
                          + (iceScore * iceWeight);

        Debug.Log($"[StarRating] Ascariasis (3-param) -> rbc={rbcScore}, wbc={wbcScore}, " +
                  $"ice={iceScore}, finalScore(pre-clamp)={finalScore}");

        finalScore = Mathf.Clamp01(finalScore);
        int stars = GetStars(finalScore, FormulaType.Ascariasis);
        Debug.Log($"[StarRating] finalScore={finalScore}, stars={stars}");

        // No completionTime available in this overload, so timeText is left as-is.
        yield return StartCoroutine(FinishEvaluation(finalScore, stars, completionTime: null));

        Debug.Log("[StarRating] Ascariasis Routine FINISHED");
    }

    /// <summary>
    /// Companion to EvaluateScore(float, float, float) — computes the Dengue
    /// score directly from RBC/WBC/Platelets values passed in, without touching
    /// valuesForStar, then reuses the same UI-update/animation path as the main
    /// EvaluateRoutine. Structurally identical to EvaluateAscariasisRoutine,
    /// just against the Dengue max/weight/threshold fields and a Platelets
    /// value in place of ICE.
    /// </summary>
    private IEnumerator EvaluateDengueRoutine(float rbcValue, float wbcValue, float plateletsValue)
    {
        Debug.Log("[StarRating] Dengue Routine STARTED");
        yield return null;

        float rbcScore = rbcMaxOxygenDeliverDengue > 0f
            ? Mathf.Clamp01(rbcValue / rbcMaxOxygenDeliverDengue)
            : 0f;

        float wbcScore = wbcMaxEnemyKilledDengue > 0
            ? Mathf.Clamp01(wbcValue / wbcMaxEnemyKilledDengue)
            : 0f;

        float plateletsScore = plateletsMaxWoundHealedDengue > 0
            ? Mathf.Clamp01(plateletsValue / plateletsMaxWoundHealedDengue)
            : 0f;

        float finalScore = (rbcScore * rbcWeightDengue)
                          + (wbcScore * wbcWeightDengue)
                          + (plateletsScore * plateletsWeightDengue);

        Debug.Log($"[StarRating] Dengue (3-param) -> rbc={rbcScore}, wbc={wbcScore}, " +
                  $"platelets={plateletsScore}, finalScore(pre-clamp)={finalScore}");

        finalScore = Mathf.Clamp01(finalScore);
        int stars = GetStars(finalScore, FormulaType.Dengue);
        Debug.Log($"[StarRating] finalScore={finalScore}, stars={stars}");

        // No completionTime available in this overload, so timeText is left as-is.
        yield return StartCoroutine(FinishEvaluation(finalScore, stars, completionTime: null));

        Debug.Log("[StarRating] Dengue Routine FINISHED");
    }

    /// <summary>
    /// Companion to EvaluateScore(float, float) — computes the Influenza score
    /// directly from RBC/WBC values passed in, without touching valuesForStar,
    /// then reuses the same UI-update/animation path as the main EvaluateRoutine.
    /// </summary>
    private IEnumerator EvaluateInfluenzaRoutine(float rbcValue, float wbcValue)
    {
        Debug.Log("[StarRating] Influenza Routine STARTED");
        yield return null;

        float rbcScore = rbcMaxOxygenDeliverInfluenza > 0f
            ? Mathf.Clamp01(rbcValue / rbcMaxOxygenDeliverInfluenza)
            : 0f;

        float wbcScore = wbcMaxEnemyKilledInfluenza > 0
            ? Mathf.Clamp01(wbcValue / wbcMaxEnemyKilledInfluenza)
            : 0f;

        float finalScore = (rbcScore * rbcWeightInfluenza)
                          + (wbcScore * wbcWeightInfluenza);

        Debug.Log($"[StarRating] Influenza (2-param) -> rbc={rbcScore}, wbc={wbcScore}, " +
                  $"finalScore(pre-clamp)={finalScore}");

        finalScore = Mathf.Clamp01(finalScore);
        int stars = GetStars(finalScore, FormulaType.Influenza);
        Debug.Log($"[StarRating] finalScore={finalScore}, stars={stars}");

        // No completionTime available in this overload, so timeText is left as-is.
        yield return StartCoroutine(FinishEvaluation(finalScore, stars, completionTime: null));

        Debug.Log("[StarRating] Influenza Routine FINISHED");
    }

    /// <summary>
    /// Companion to EvaluateScore(float, float) — computes the Pneumococcal score
    /// directly from RBC/WBC values passed in, without touching valuesForStar,
    /// then reuses the same UI-update/animation path as the main EvaluateRoutine.
    /// Structurally identical to EvaluateInfluenzaRoutine, just against the
    /// Pneumococcal max/weight/threshold fields so this scene can be tuned
    /// independently of any Influenza scene.
    /// </summary>
    private IEnumerator EvaluatePneumococcalRoutine(float rbcValue, float wbcValue)
    {
        Debug.Log("[StarRating] Pneumococcal Routine STARTED");
        yield return null;

        float rbcScore = rbcMaxOxygenDeliverPneumococcal > 0f
            ? Mathf.Clamp01(rbcValue / rbcMaxOxygenDeliverPneumococcal)
            : 0f;

        float wbcScore = wbcMaxEnemyKilledPneumococcal > 0
            ? Mathf.Clamp01(wbcValue / wbcMaxEnemyKilledPneumococcal)
            : 0f;

        float finalScore = (rbcScore * rbcWeightPneumococcal)
                          + (wbcScore * wbcWeightPneumococcal);

        Debug.Log($"[StarRating] Pneumococcal (2-param) -> rbc={rbcScore}, wbc={wbcScore}, " +
                  $"finalScore(pre-clamp)={finalScore}");

        finalScore = Mathf.Clamp01(finalScore);
        int stars = GetStars(finalScore, FormulaType.Pneumococcal);
        Debug.Log($"[StarRating] finalScore={finalScore}, stars={stars}");

        // No completionTime available in this overload, so timeText is left as-is.
        yield return StartCoroutine(FinishEvaluation(finalScore, stars, completionTime: null));

        Debug.Log("[StarRating] Pneumococcal Routine FINISHED");
    }

    /// <summary>
    /// Shared tail end of both evaluation routines: updates score/time/feedback
    /// text and plays the star fill animation. completionTime is nullable since
    /// the RBC/WBC(/ICE) overloads have no time value to show.
    /// </summary>
    private IEnumerator FinishEvaluation(float finalScore, int stars, float? completionTime)
    {
        if (scoreText == null || timeText == null || feedbackText == null)
        {
            Debug.LogError("[StarRating] One or more Text fields (scoreText/timeText/feedbackText) " +
                "are NOT assigned in the Inspector. Assign them or this will throw a NullReferenceException.");
            yield break;
        }

        scoreText.text = "Score: " + Mathf.RoundToInt(finalScore * 100);
        timeText.text = completionTime.HasValue ? "Time: " + FormatTime(completionTime.Value) : "";
        feedbackText.text = GetFeedback(stars);

        Debug.Log($"[StarRating] UI text set -> scoreText.text='{scoreText.text}' " +
                  $"(GO active in hierarchy: {scoreText.gameObject.activeInHierarchy}, " +
                  $"alpha: {scoreText.color.a})");

        if (starImages == null || starImages.Length == 0)
        {
            Debug.LogError("[StarRating] starImages array is empty/unassigned — no stars to animate.");
            yield break;
        }

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null)
            {
                Debug.LogError($"[StarRating] starImages[{i}] is unassigned in the Inspector.");
                continue;
            }
            starImages[i].sprite = GetEmpty(i);
            starImages[i].transform.localScale = Vector3.one;
        }

        yield return StartCoroutine(AnimateStars(stars));
    }

    /// <summary>
    /// Picks which threshold set to compare against depending on which formula
    /// produced the score, since Formula 1, Ascariasis, and Influenza scores
    /// aren't necessarily comparable and may need different cutoffs for the
    /// same star count.
    /// </summary>
    private int GetStars(float score, FormulaType formula)
    {
        float threshold3;
        float threshold2;
        float threshold1;

        switch (formula)
        {
            case FormulaType.Ascariasis:
                threshold3 = threshold3StarsAscariasis;
                threshold2 = threshold2StarsAscariasis;
                threshold1 = threshold1StarAscariasis;
                break;
            case FormulaType.Influenza:
                threshold3 = threshold3StarsInfluenza;
                threshold2 = threshold2StarsInfluenza;
                threshold1 = threshold1StarInfluenza;
                break;
            case FormulaType.Pneumococcal:
                threshold3 = threshold3StarsPneumococcal;
                threshold2 = threshold2StarsPneumococcal;
                threshold1 = threshold1StarPneumococcal;
                break;
            case FormulaType.Dengue:
                threshold3 = threshold3StarsDengue;
                threshold2 = threshold2StarsDengue;
                threshold1 = threshold1StarDengue;
                break;
            default:
                threshold3 = threshold3StarsFormula1;
                threshold2 = threshold2StarsFormula1;
                threshold1 = threshold1StarFormula1;
                break;
        }

        if (score >= threshold3) return 3;
        if (score >= threshold2) return 2;
        if (score >= threshold1) return 1;
        return 0;
    }

    private IEnumerator AnimateStars(int earnedStars)
    {
        for (int i = 0; i < starImages.Length; i++)
        {
            yield return new WaitForSeconds(delayBetweenStars);

            if (i < earnedStars)
            {
                if (starImages[i] == null) continue;
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