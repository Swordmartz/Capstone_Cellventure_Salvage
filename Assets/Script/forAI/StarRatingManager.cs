using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StarRatingManager : MonoBehaviour
{
    // ============================================================
    // STARS
    // ============================================================

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


    // ============================================================
    // UI TEXT
    // ============================================================

    [Header("UI Text")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI feedbackText;


    // ============================================================
    // STAR ANIMATION
    // ============================================================

    [Header("Star Animation Settings")]

    [Tooltip("Normal size of the star.")]
    public float starNormalScale = 1f;

    [Tooltip("Maximum size during the pop animation.")]
    public float starPopScale = 1.25f;

    [Tooltip("How long one star animation takes.")]
    public float animDuration = 0.6f;

    [Tooltip("Delay before the next star animates.")]
    public float delayBetweenStars = 0.3f;


    // ============================================================
    // LEVEL SETTINGS
    // ============================================================

    [Header("Level Settings")]

    [Tooltip("Maximum time remaining used to normalize Formula 1.")]
    public float maxTime = 120f;


    // ============================================================
    // ACHIEVEMENT POPUP
    // ============================================================

    [Header("Achievement Popup")]

    [Tooltip("Enable the achievement panel.")]
    public bool enableAchievementPopup = true;

    [Tooltip(
        "The RectTransform of the achievement panel. " +
        "Place it where you want it to FINISH."
    )]
    public RectTransform achievementPanel;

    [Tooltip(
        "How far above the final position the panel starts."
    )]
    public float achievementStartOffsetY = 300f;

    [Tooltip(
        "How long the achievement panel takes to slide down."
    )]
    public float achievementAnimDuration = 0.5f;

    [Tooltip(
        "Time remaining required for the achievement. " +
        "Set to 0 to always trigger the achievement."
    )]
    public float achievementTimeThreshold = 0f;

    [Tooltip(
        "Reset and hide the achievement panel before every evaluation."
    )]
    public bool resetAchievementPanelEachEvaluation = true;


    // ============================================================
    // OPTIONAL DIRECT MISSION REFERENCE
    // ============================================================

    [Header("Mission Reference")]

    [Tooltip(
        "Optional AI_TestTD reference. " +
        "This is used by the parameterless EvaluateFromMission() " +
        "UnityEvent."
    )]
    public AI_TestTD missionDataReference;


    // ============================================================
    // FORMULA SELECTION
    // ============================================================

    [Header("Formula Selection")]

    public bool useFormula1 = true;

    public bool useFormulaAscariasis = false;

    public bool useFormulaInfluenza = false;

    public bool useFormulaPneumococcal = false;

    public bool useFormulaDengue = false;


    // ============================================================
    // FORMULA 1
    // ============================================================

    [Header("Formula 1 — Time + Performance + Idle + Delivery")]

    public float timeWeight = 0.35f;

    public float performanceWeight = 0.35f;

    public float idleWeight = 0.15f;

    public float failedDeliveryWeight = 0.15f;

    public float idlePenaltyPerSecond = 0.05f;

    public float failedDeliveryPenalty = 0.2f;


    // ============================================================
    // ASCARIASIS
    // ============================================================

    [Header("Formula - Ascariasis — RBC + WBC + ICE")]

    public ValuesForStar valuesForStar;

    public float rbcMaxOxygenDeliver = 3f;

    public int wbcMaxEnemyKilled = 1;

    public float iceMaxBarValue = 60f;

    public float rbcWeight = 0.34f;

    public float wbcWeight = 0.33f;

    public float iceWeight = 0.33f;


    // ============================================================
    // INFLUENZA
    // ============================================================

    [Header("Formula - Influenza — RBC + WBC")]

    public float rbcMaxOxygenDeliverInfluenza = 3f;

    public int wbcMaxEnemyKilledInfluenza = 1;

    public float rbcWeightInfluenza = 0.5f;

    public float wbcWeightInfluenza = 0.5f;


    // ============================================================
    // PNEUMOCOCCAL
    // ============================================================

    [Header("Formula - Pneumococcal — RBC + WBC")]

    public float rbcMaxOxygenDeliverPneumococcal = 3f;

    public int wbcMaxEnemyKilledPneumococcal = 1;

    public float rbcWeightPneumococcal = 0.5f;

    public float wbcWeightPneumococcal = 0.5f;


    // ============================================================
    // DENGUE
    // ============================================================

    [Header("Formula - Dengue — RBC + WBC + Platelets")]

    public float rbcMaxOxygenDeliverDengue = 3f;

    public int wbcMaxEnemyKilledDengue = 1;

    public int plateletsMaxWoundHealedDengue = 1;

    public float rbcWeightDengue = 0.34f;

    public float wbcWeightDengue = 0.33f;

    public float plateletsWeightDengue = 0.33f;


    // ============================================================
    // STAR THRESHOLDS
    // ============================================================

    [Header("Star Thresholds — Formula 1")]

    [Range(0f, 1f)]
    public float threshold3StarsFormula1 = 0.80f;

    [Range(0f, 1f)]
    public float threshold2StarsFormula1 = 0.50f;

    [Range(0f, 1f)]
    public float threshold1StarFormula1 = 0.20f;


    [Header("Star Thresholds — Ascariasis")]

    [Range(0f, 1f)]
    public float threshold3StarsAscariasis = 0.80f;

    [Range(0f, 1f)]
    public float threshold2StarsAscariasis = 0.50f;

    [Range(0f, 1f)]
    public float threshold1StarAscariasis = 0.20f;


    [Header("Star Thresholds — Influenza")]

    [Range(0f, 1f)]
    public float threshold3StarsInfluenza = 0.80f;

    [Range(0f, 1f)]
    public float threshold2StarsInfluenza = 0.50f;

    [Range(0f, 1f)]
    public float threshold1StarInfluenza = 0.20f;


    [Header("Star Thresholds — Pneumococcal")]

    [Range(0f, 1f)]
    public float threshold3StarsPneumococcal = 0.80f;

    [Range(0f, 1f)]
    public float threshold2StarsPneumococcal = 0.50f;

    [Range(0f, 1f)]
    public float threshold1StarPneumococcal = 0.20f;


    [Header("Star Thresholds — Dengue")]

    [Range(0f, 1f)]
    public float threshold3StarsDengue = 0.80f;

    [Range(0f, 1f)]
    public float threshold2StarsDengue = 0.50f;

    [Range(0f, 1f)]
    public float threshold1StarDengue = 0.20f;


    // ============================================================
    // INTERNAL
    // ============================================================

    private enum FormulaType
    {
        Formula1,
        Ascariasis,
        Influenza,
        Pneumococcal,
        Dengue
    }

    private Vector2 achievementRestingPosition;

    private bool achievementPositionInitialized = false;


    // ============================================================
    // VALIDATION
    // ============================================================

    private void OnValidate()
    {
        int checkedCount =
            (useFormula1 ? 1 : 0) +
            (useFormulaAscariasis ? 1 : 0) +
            (useFormulaInfluenza ? 1 : 0) +
            (useFormulaPneumococcal ? 1 : 0) +
            (useFormulaDengue ? 1 : 0);

        if (checkedCount > 1)
        {
            Debug.LogWarning(
                "[StarRating] More than one formula is selected. " +
                "Only ONE formula should be checked."
            );
        }

        if (checkedCount == 0)
        {
            Debug.LogWarning(
                "[StarRating] No formula is selected."
            );
        }
    }


    // ============================================================
    // SPRITES
    // ============================================================

    private Sprite GetFilled(int index)
    {
        switch (index)
        {
            case 0:
                return star1Filled;

            case 1:
                return star2Filled;

            case 2:
                return star3Filled;

            default:
                return null;
        }
    }


    private Sprite GetEmpty(int index)
    {
        switch (index)
        {
            case 0:
                return star1Empty;

            case 1:
                return star2Empty;

            case 2:
                return star3Empty;

            default:
                return null;
        }
    }


    // ============================================================
    // UNITY EVENT VERSION
    // ============================================================
    //
    // THIS IS THE IMPORTANT CHANGE FOR YOUR SCREENSHOT.
    //
    // In your UnityEvent, you can now select:
    //
    // StarRatingManager
    //      -> EvaluateFromMission()
    //
    // without needing to pass AI_TestTD manually.
    //
    // ============================================================

    public void EvaluateFromMission()
    {
        Debug.Log(
            "[StarRating] Parameterless EvaluateFromMission() called."
        );

        if (missionDataReference == null)
        {
            Debug.LogError(
                "[StarRating] Mission Data Reference is NOT assigned!"
            );

            Debug.LogError(
                "[StarRating] Assign the AI_TestTD component " +
                "to Mission Data Reference in the Inspector."
            );

            return;
        }

        EvaluateFromMission(missionDataReference);
    }


    // ============================================================
    // VERSION THAT ACCEPTS AI_TESTTD
    // ============================================================

    public void EvaluateFromMission(AI_TestTD missionData)
    {
        Debug.Log(
            "[StarRating] EvaluateFromMission(AI_TestTD) called."
        );

        if (missionData == null)
        {
            Debug.LogError(
                "[StarRating] AI_TestTD reference is NULL."
            );

            return;
        }

        Debug.Log(
            $"[StarRating] Mission Data | " +
            $"Time={missionData.comptTime} | " +
            $"Performance={missionData.performanceScore} | " +
            $"Idle={missionData.idleTime} | " +
            $"Failed={missionData.FailedDelivery}"
        );

        EvaluateScore(
            missionData.comptTime,
            missionData.performanceScore,
            missionData.idleTime,
            missionData.FailedDelivery
        );
    }


    // ============================================================
    // MAIN EVALUATE SCORE
    // ============================================================

    public void EvaluateScore(
        float completionTime,
        float performanceScore,
        int idleTime,
        int failedDeliveries)
    {
        Debug.Log(
            $"[StarRating] EvaluateScore called | " +
            $"Time={completionTime} | " +
            $"Performance={performanceScore} | " +
            $"Idle={idleTime} | " +
            $"Failed={failedDeliveries}"
        );

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "[StarRating] StarRatingManager GameObject is inactive!"
            );

            return;
        }

        StopAllCoroutines();

        StartCoroutine(
            EvaluateRoutine(
                completionTime,
                performanceScore,
                idleTime,
                failedDeliveries
            )
        );
    }


    // ============================================================
    // RBC + WBC + THIRD VALUE
    // ============================================================

    public void EvaluateScore(
        float rbcValue,
        float wbcValue,
        float thirdValue)
    {
        Debug.Log(
            $"[StarRating] RBC/WBC/Third Evaluation | " +
            $"RBC={rbcValue} | WBC={wbcValue} | Third={thirdValue}"
        );

        int checkedCount =
            (useFormulaAscariasis ? 1 : 0) +
            (useFormulaDengue ? 1 : 0);

        if (checkedCount == 0)
        {
            Debug.LogError(
                "[StarRating] Neither Ascariasis nor Dengue is selected."
            );

            return;
        }

        if (checkedCount > 1)
        {
            Debug.LogError(
                "[StarRating] Both Ascariasis and Dengue are selected."
            );

            return;
        }

        StopAllCoroutines();

        if (useFormulaAscariasis)
        {
            StartCoroutine(
                EvaluateAscariasisRoutine(
                    rbcValue,
                    wbcValue,
                    thirdValue
                )
            );
        }
        else
        {
            StartCoroutine(
                EvaluateDengueRoutine(
                    rbcValue,
                    wbcValue,
                    thirdValue
                )
            );
        }
    }


    // ============================================================
    // RBC + WBC
    // ============================================================

    public void EvaluateScore(
        float rbcValue,
        float wbcValue)
    {
        Debug.Log(
            $"[StarRating] RBC/WBC Evaluation | " +
            $"RBC={rbcValue} | WBC={wbcValue}"
        );

        int checkedCount =
            (useFormulaInfluenza ? 1 : 0) +
            (useFormulaPneumococcal ? 1 : 0);

        if (checkedCount == 0)
        {
            Debug.LogError(
                "[StarRating] Neither Influenza nor Pneumococcal is selected."
            );

            return;
        }

        if (checkedCount > 1)
        {
            Debug.LogError(
                "[StarRating] Both Influenza and Pneumococcal are selected."
            );

            return;
        }

        StopAllCoroutines();

        if (useFormulaInfluenza)
        {
            StartCoroutine(
                EvaluateInfluenzaRoutine(
                    rbcValue,
                    wbcValue
                )
            );
        }
        else
        {
            StartCoroutine(
                EvaluatePneumococcalRoutine(
                    rbcValue,
                    wbcValue
                )
            );
        }
    }


    // ============================================================
    // FORMULA 1 ROUTINE
    // ============================================================

    private IEnumerator EvaluateRoutine(
        float completionTime,
        float performanceScore,
        int idleTime,
        int failedDeliveries)
    {
        Debug.Log(
            "[StarRating] Evaluation Routine STARTED."
        );

        yield return null;

        int checkedCount =
            (useFormula1 ? 1 : 0) +
            (useFormulaAscariasis ? 1 : 0) +
            (useFormulaInfluenza ? 1 : 0) +
            (useFormulaPneumococcal ? 1 : 0) +
            (useFormulaDengue ? 1 : 0);

        if (checkedCount == 0)
        {
            Debug.LogError(
                "[StarRating] No formula is selected!"
            );

            yield break;
        }

        if (checkedCount > 1)
        {
            Debug.LogError(
                "[StarRating] More than one formula is selected!"
            );

            yield break;
        }

        float finalScore = 0f;

        FormulaType formulaUsed =
            FormulaType.Formula1;


        // ========================================================
        // FORMULA 1
        // ========================================================

        if (useFormula1)
        {
            float timeScore =
                maxTime > 0f
                ? Mathf.Clamp01(
                    completionTime / maxTime
                )
                : 0f;

            float perfScore =
                Mathf.Clamp01(
                    performanceScore / 15f
                );

            float idleScore =
                Mathf.Clamp01(
                    1f -
                    idleTime *
                    idlePenaltyPerSecond
                );

            float deliveryScore =
                Mathf.Clamp01(
                    1f -
                    failedDeliveries *
                    failedDeliveryPenalty
                );

            finalScore =
                (timeScore * timeWeight) +
                (perfScore * performanceWeight) +
                (idleScore * idleWeight) +
                (deliveryScore * failedDeliveryWeight);

            formulaUsed =
                FormulaType.Formula1;

            Debug.Log(
                $"[StarRating] Formula 1 | " +
                $"Time={timeScore:F2} | " +
                $"Performance={perfScore:F2} | " +
                $"Idle={idleScore:F2} | " +
                $"Delivery={deliveryScore:F2} | " +
                $"Final={finalScore:F2}"
            );
        }


        // ========================================================
        // ASCARIASIS
        // ========================================================

        else if (useFormulaAscariasis)
        {
            if (valuesForStar == null)
            {
                Debug.LogError(
                    "[StarRating] ValuesForStar is not assigned."
                );

                yield break;
            }

            float rbcScore =
                rbcMaxOxygenDeliver > 0f
                ? Mathf.Clamp01(
                    valuesForStar.OxygenDeliver /
                    rbcMaxOxygenDeliver
                )
                : 0f;

            float wbcScore =
                wbcMaxEnemyKilled > 0
                ? Mathf.Clamp01(
                    (float)valuesForStar.EnemyKilled /
                    wbcMaxEnemyKilled
                )
                : 0f;

            float iceScore =
                iceMaxBarValue > 0f
                ? Mathf.Clamp01(
                    valuesForStar.BarValue /
                    iceMaxBarValue
                )
                : 0f;

            finalScore =
                (rbcScore * rbcWeight) +
                (wbcScore * wbcWeight) +
                (iceScore * iceWeight);

            formulaUsed =
                FormulaType.Ascariasis;
        }


        // ========================================================
        // INFLUENZA
        // ========================================================

        else if (useFormulaInfluenza)
        {
            if (valuesForStar == null)
            {
                Debug.LogError(
                    "[StarRating] ValuesForStar is not assigned."
                );

                yield break;
            }

            float rbcScore =
                rbcMaxOxygenDeliverInfluenza > 0f
                ? Mathf.Clamp01(
                    valuesForStar.OxygenDeliver /
                    rbcMaxOxygenDeliverInfluenza
                )
                : 0f;

            float wbcScore =
                wbcMaxEnemyKilledInfluenza > 0
                ? Mathf.Clamp01(
                    (float)valuesForStar.EnemyKilled /
                    wbcMaxEnemyKilledInfluenza
                )
                : 0f;

            finalScore =
                (rbcScore * rbcWeightInfluenza) +
                (wbcScore * wbcWeightInfluenza);

            formulaUsed =
                FormulaType.Influenza;
        }


        // ========================================================
        // PNEUMOCOCCAL
        // ========================================================

        else if (useFormulaPneumococcal)
        {
            if (valuesForStar == null)
            {
                Debug.LogError(
                    "[StarRating] ValuesForStar is not assigned."
                );

                yield break;
            }

            float rbcScore =
                rbcMaxOxygenDeliverPneumococcal > 0f
                ? Mathf.Clamp01(
                    valuesForStar.OxygenDeliver /
                    rbcMaxOxygenDeliverPneumococcal
                )
                : 0f;

            float wbcScore =
                wbcMaxEnemyKilledPneumococcal > 0
                ? Mathf.Clamp01(
                    (float)valuesForStar.EnemyKilled /
                    wbcMaxEnemyKilledPneumococcal
                )
                : 0f;

            finalScore =
                (rbcScore * rbcWeightPneumococcal) +
                (wbcScore * wbcWeightPneumococcal);

            formulaUsed =
                FormulaType.Pneumococcal;
        }


        // ========================================================
        // DENGUE
        // ========================================================

        else if (useFormulaDengue)
        {
            if (valuesForStar == null)
            {
                Debug.LogError(
                    "[StarRating] ValuesForStar is not assigned."
                );

                yield break;
            }

            float rbcScore =
                rbcMaxOxygenDeliverDengue > 0f
                ? Mathf.Clamp01(
                    valuesForStar.OxygenDeliver /
                    rbcMaxOxygenDeliverDengue
                )
                : 0f;

            float wbcScore =
                wbcMaxEnemyKilledDengue > 0
                ? Mathf.Clamp01(
                    (float)valuesForStar.EnemyKilled /
                    wbcMaxEnemyKilledDengue
                )
                : 0f;

            float plateletsScore =
                plateletsMaxWoundHealedDengue > 0
                ? Mathf.Clamp01(
                    (float)valuesForStar.WoundHealed /
                    plateletsMaxWoundHealedDengue
                )
                : 0f;

            finalScore =
                (rbcScore * rbcWeightDengue) +
                (wbcScore * wbcWeightDengue) +
                (plateletsScore * plateletsWeightDengue);

            formulaUsed =
                FormulaType.Dengue;
        }


        // ========================================================
        // FINAL SCORE
        // ========================================================

        finalScore =
            Mathf.Clamp01(finalScore);

        int stars =
            GetStars(
                finalScore,
                formulaUsed
            );

        Debug.Log(
            $"[StarRating] FINAL SCORE = " +
            $"{finalScore:F2} | " +
            $"STARS = {stars}"
        );

        yield return StartCoroutine(
            FinishEvaluation(
                finalScore,
                stars,
                completionTime
            )
        );

        Debug.Log(
            "[StarRating] Evaluation Routine FINISHED."
        );
    }


    // ============================================================
    // ASCARIASIS
    // ============================================================

    private IEnumerator EvaluateAscariasisRoutine(
        float rbcValue,
        float wbcValue,
        float iceValue)
    {
        yield return null;

        float rbcScore =
            rbcMaxOxygenDeliver > 0f
            ? Mathf.Clamp01(
                rbcValue /
                rbcMaxOxygenDeliver
            )
            : 0f;

        float wbcScore =
            wbcMaxEnemyKilled > 0
            ? Mathf.Clamp01(
                wbcValue /
                wbcMaxEnemyKilled
            )
            : 0f;

        float iceScore =
            iceMaxBarValue > 0f
            ? Mathf.Clamp01(
                iceValue /
                iceMaxBarValue
            )
            : 0f;

        float finalScore =
            (rbcScore * rbcWeight) +
            (wbcScore * wbcWeight) +
            (iceScore * iceWeight);

        finalScore =
            Mathf.Clamp01(finalScore);

        int stars =
            GetStars(
                finalScore,
                FormulaType.Ascariasis
            );

        yield return StartCoroutine(
            FinishEvaluation(
                finalScore,
                stars,
                null
            )
        );
    }


    // ============================================================
    // DENGUE
    // ============================================================

    private IEnumerator EvaluateDengueRoutine(
        float rbcValue,
        float wbcValue,
        float plateletsValue)
    {
        yield return null;

        float rbcScore =
            rbcMaxOxygenDeliverDengue > 0f
            ? Mathf.Clamp01(
                rbcValue /
                rbcMaxOxygenDeliverDengue
            )
            : 0f;

        float wbcScore =
            wbcMaxEnemyKilledDengue > 0
            ? Mathf.Clamp01(
                wbcValue /
                wbcMaxEnemyKilledDengue
            )
            : 0f;

        float plateletsScore =
            plateletsMaxWoundHealedDengue > 0
            ? Mathf.Clamp01(
                plateletsValue /
                plateletsMaxWoundHealedDengue
            )
            : 0f;

        float finalScore =
            (rbcScore * rbcWeightDengue) +
            (wbcScore * wbcWeightDengue) +
            (plateletsScore * plateletsWeightDengue);

        finalScore =
            Mathf.Clamp01(finalScore);

        int stars =
            GetStars(
                finalScore,
                FormulaType.Dengue
            );

        yield return StartCoroutine(
            FinishEvaluation(
                finalScore,
                stars,
                null
            )
        );
    }


    // ============================================================
    // INFLUENZA
    // ============================================================

    private IEnumerator EvaluateInfluenzaRoutine(
        float rbcValue,
        float wbcValue)
    {
        yield return null;

        float rbcScore =
            rbcMaxOxygenDeliverInfluenza > 0f
            ? Mathf.Clamp01(
                rbcValue /
                rbcMaxOxygenDeliverInfluenza
            )
            : 0f;

        float wbcScore =
            wbcMaxEnemyKilledInfluenza > 0
            ? Mathf.Clamp01(
                wbcValue /
                wbcMaxEnemyKilledInfluenza
            )
            : 0f;

        float finalScore =
            (rbcScore * rbcWeightInfluenza) +
            (wbcScore * wbcWeightInfluenza);

        finalScore =
            Mathf.Clamp01(finalScore);

        int stars =
            GetStars(
                finalScore,
                FormulaType.Influenza
            );

        yield return StartCoroutine(
            FinishEvaluation(
                finalScore,
                stars,
                null
            )
        );
    }


    // ============================================================
    // PNEUMOCOCCAL
    // ============================================================

    private IEnumerator EvaluatePneumococcalRoutine(
        float rbcValue,
        float wbcValue)
    {
        yield return null;

        float rbcScore =
            rbcMaxOxygenDeliverPneumococcal > 0f
            ? Mathf.Clamp01(
                rbcValue /
                rbcMaxOxygenDeliverPneumococcal
            )
            : 0f;

        float wbcScore =
            wbcMaxEnemyKilledPneumococcal > 0
            ? Mathf.Clamp01(
                wbcValue /
                wbcMaxEnemyKilledPneumococcal
            )
            : 0f;

        float finalScore =
            (rbcScore * rbcWeightPneumococcal) +
            (wbcScore * wbcWeightPneumococcal);

        finalScore =
            Mathf.Clamp01(finalScore);

        int stars =
            GetStars(
                finalScore,
                FormulaType.Pneumococcal
            );

        yield return StartCoroutine(
            FinishEvaluation(
                finalScore,
                stars,
                null
            )
        );
    }


    // ============================================================
    // FINISH EVALUATION
    // ============================================================

    private IEnumerator FinishEvaluation(
        float finalScore,
        int stars,
        float? completionTime)
    {
        Debug.Log(
            $"[StarRating] Finishing evaluation | " +
            $"Score={finalScore:F2} | Stars={stars}"
        );


        // --------------------------------------------------------
        // RESET ACHIEVEMENT
        // --------------------------------------------------------

        if (
            enableAchievementPopup &&
            resetAchievementPanelEachEvaluation
        )
        {
            ResetAchievementPanel();
        }


        // --------------------------------------------------------
        // SCORE TEXT
        // --------------------------------------------------------

        if (scoreText != null)
        {
            int score =
                Mathf.RoundToInt(
                    finalScore * 100f
                );

            scoreText.text =
                "Score: " + score;

            scoreText.gameObject.SetActive(true);

            Debug.Log(
                "[StarRating] Score displayed: " +
                scoreText.text
            );
        }
        else
        {
            Debug.LogError(
                "[StarRating] scoreText is NOT assigned!"
            );
        }


        // --------------------------------------------------------
        // TIME TEXT
        // --------------------------------------------------------

        if (timeText != null)
        {
            if (completionTime.HasValue)
            {
                timeText.text =
                    "Time Left: " +
                    FormatTime(
                        completionTime.Value
                    );
            }
            else
            {
                timeText.text = "";
            }

            timeText.gameObject.SetActive(true);
        }


        // --------------------------------------------------------
        // FEEDBACK
        // --------------------------------------------------------

        if (feedbackText != null)
        {
            feedbackText.text =
                GetFeedback(stars);

            feedbackText.gameObject.SetActive(true);
        }


        // --------------------------------------------------------
        // CHECK STARS
        // --------------------------------------------------------

        if (
            starImages == null ||
            starImages.Length < 3
        )
        {
            Debug.LogError(
                "[StarRating] You need 3 Star Images assigned!"
            );

            yield break;
        }


        // --------------------------------------------------------
        // RESET STAR VISUALS
        // --------------------------------------------------------

        for (int i = 0; i < 3; i++)
        {
            if (starImages[i] == null)
            {
                Debug.LogError(
                    $"[StarRating] Star {i + 1} is not assigned!"
                );

                continue;
            }

            starImages[i].gameObject.SetActive(true);

            starImages[i].sprite =
                GetEmpty(i);

            starImages[i].rectTransform.localScale =
                Vector3.zero;
        }


        // --------------------------------------------------------
        // ANIMATE STARS
        // --------------------------------------------------------

        yield return StartCoroutine(
            AnimateStars(stars)
        );


        // --------------------------------------------------------
        // ACHIEVEMENT
        // --------------------------------------------------------

        if (
            enableAchievementPopup &&
            completionTime.HasValue &&
            completionTime.Value >=
            achievementTimeThreshold
        )
        {
            Debug.Log(
                $"[StarRating] Achievement triggered! " +
                $"Time={completionTime.Value} " +
                $"Threshold={achievementTimeThreshold}"
            );

            yield return StartCoroutine(
                ShowAchievementPopup()
            );
        }
        else
        {
            Debug.Log(
                "[StarRating] Achievement was not triggered."
            );
        }
    }


    // ============================================================
    // STAR ANIMATION
    // ============================================================

    private IEnumerator AnimateStars(
        int earnedStars)
    {
        earnedStars =
            Mathf.Clamp(
                earnedStars,
                0,
                3
            );

        Debug.Log(
            $"[StarRating] Animating {earnedStars} stars."
        );

        for (int i = 0; i < earnedStars; i++)
        {
            if (starImages[i] == null)
                continue;

            starImages[i].sprite =
                GetFilled(i);

            starImages[i].rectTransform.localScale =
                Vector3.zero;

            yield return StartCoroutine(
                AnimateStar(
                    starImages[i].rectTransform
                )
            );

            if (i <
                earnedStars - 1)
            {
                yield return new WaitForSeconds(
                    delayBetweenStars
                );
            }
        }
    }


    // ============================================================
    // INDIVIDUAL STAR ANIMATION
    // ============================================================

    private IEnumerator AnimateStar(
        RectTransform starTransform)
    {
        if (starTransform == null)
            yield break;

        float halfDuration =
            animDuration * 0.5f;


        // --------------------------------------------------------
        // ZERO -> POP
        // --------------------------------------------------------

        float elapsed = 0f;

        Vector3 zero =
            Vector3.zero;

        Vector3 pop =
            Vector3.one *
            starPopScale;

        Vector3 normal =
            Vector3.one *
            starNormalScale;


        while (
            elapsed <
            halfDuration
        )
        {
            elapsed +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    halfDuration
                );

            float eased =
                EaseOutBack(t);

            starTransform.localScale =
                Vector3.LerpUnclamped(
                    zero,
                    pop,
                    eased
                );

            yield return null;
        }


        // --------------------------------------------------------
        // POP -> NORMAL
        // --------------------------------------------------------

        elapsed = 0f;

        while (
            elapsed <
            halfDuration
        )
        {
            elapsed +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    halfDuration
                );

            starTransform.localScale =
                Vector3.Lerp(
                    pop,
                    normal,
                    t
                );

            yield return null;
        }


        // Guarantee final size
        starTransform.localScale =
            normal;
    }


    // ============================================================
    // ACHIEVEMENT PANEL
    // ============================================================

    private IEnumerator ShowAchievementPopup()
    {
        if (achievementPanel == null)
        {
            Debug.LogError(
                "[StarRating] Achievement Panel is NOT assigned!"
            );

            yield break;
        }


        // --------------------------------------------------------
        // SAVE RESTING POSITION
        // --------------------------------------------------------

        if (!achievementPositionInitialized)
        {
            achievementRestingPosition =
                achievementPanel.anchoredPosition;

            achievementPositionInitialized =
                true;
        }


        Vector2 target =
            achievementRestingPosition;


        Vector2 start =
            target +
            Vector2.up *
            achievementStartOffsetY;


        // --------------------------------------------------------
        // ENABLE
        // --------------------------------------------------------

        achievementPanel.gameObject.SetActive(
            true
        );


        // --------------------------------------------------------
        // START ABOVE
        // --------------------------------------------------------

        achievementPanel.anchoredPosition =
            start;


        Debug.Log(
            $"[Achievement] Starting at {start}"
        );

        Debug.Log(
            $"[Achievement] Target position is {target}"
        );


        // --------------------------------------------------------
        // ANIMATE DOWN
        // --------------------------------------------------------

        float elapsed = 0f;

        while (
            elapsed <
            achievementAnimDuration
        )
        {
            elapsed +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    achievementAnimDuration
                );

            float eased =
                EaseOutBack(t);

            achievementPanel.anchoredPosition =
                Vector2.LerpUnclamped(
                    start,
                    target,
                    eased
                );

            yield return null;
        }


        // --------------------------------------------------------
        // GUARANTEE TARGET
        // --------------------------------------------------------

        achievementPanel.anchoredPosition =
            target;

        Debug.Log(
            "[Achievement] Panel animation COMPLETE."
        );
    }


    // ============================================================
    // RESET ACHIEVEMENT
    // ============================================================

    private void ResetAchievementPanel()
    {
        if (achievementPanel == null)
            return;


        if (!achievementPositionInitialized)
        {
            achievementRestingPosition =
                achievementPanel.anchoredPosition;

            achievementPositionInitialized =
                true;
        }


        Vector2 hiddenPosition =
            achievementRestingPosition +
            Vector2.up *
            achievementStartOffsetY;


        achievementPanel.anchoredPosition =
            hiddenPosition;


        achievementPanel.gameObject.SetActive(
            false
        );


        Debug.Log(
            "[Achievement] Panel reset."
        );
    }


    // ============================================================
    // STAR CALCULATION
    // ============================================================

    private int GetStars(
        float score,
        FormulaType formula)
    {
        float threshold3;
        float threshold2;
        float threshold1;


        switch (formula)
        {
            case FormulaType.Ascariasis:

                threshold3 =
                    threshold3StarsAscariasis;

                threshold2 =
                    threshold2StarsAscariasis;

                threshold1 =
                    threshold1StarAscariasis;

                break;


            case FormulaType.Influenza:

                threshold3 =
                    threshold3StarsInfluenza;

                threshold2 =
                    threshold2StarsInfluenza;

                threshold1 =
                    threshold1StarInfluenza;

                break;


            case FormulaType.Pneumococcal:

                threshold3 =
                    threshold3StarsPneumococcal;

                threshold2 =
                    threshold2StarsPneumococcal;

                threshold1 =
                    threshold1StarPneumococcal;

                break;


            case FormulaType.Dengue:

                threshold3 =
                    threshold3StarsDengue;

                threshold2 =
                    threshold2StarsDengue;

                threshold1 =
                    threshold1StarDengue;

                break;


            default:

                threshold3 =
                    threshold3StarsFormula1;

                threshold2 =
                    threshold2StarsFormula1;

                threshold1 =
                    threshold1StarFormula1;

                break;
        }


        if (score >= threshold3)
            return 3;

        if (score >= threshold2)
            return 2;

        if (score >= threshold1)
            return 1;

        return 0;
    }


    // ============================================================
    // EASING
    // ============================================================

    private float EaseOutBack(
        float t)
    {
        float c1 =
            1.70158f;

        float c3 =
            c1 + 1f;

        return
            1f +
            c3 *
            Mathf.Pow(
                t - 1f,
                3f
            ) +
            c1 *
            Mathf.Pow(
                t - 1f,
                2f
            );
    }


    // ============================================================
    // FORMAT TIME
    // ============================================================

    private string FormatTime(
        float seconds)
    {
        seconds =
            Mathf.Max(
                0f,
                seconds
            );

        int minutes =
            Mathf.FloorToInt(
                seconds / 60f
            );

        int remainingSeconds =
            Mathf.FloorToInt(
                seconds % 60f
            );

        return string.Format(
            "{0:00}:{1:00}",
            minutes,
            remainingSeconds
        );
    }


    // ============================================================
    // FEEDBACK
    // ============================================================

    private string GetFeedback(
        int stars)
    {
        switch (stars)
        {
            case 3:
                return "Excellent!";

            case 2:
                return "Good Job!";

            case 1:
                return "Keep Trying!";

            default:
                return "Try Again!";
        }
    }
}