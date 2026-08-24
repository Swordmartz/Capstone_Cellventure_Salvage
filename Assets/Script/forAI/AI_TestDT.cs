using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class AI_TestTD : MonoBehaviour
{
    [Header("Reference")]
    public AIforDialogue dialogueSystem;
    public AIforGuide guideSystem;
    public GameObject oxygen;
    public LayerMask enemyLayer;
    public GameObject MB;
    private bool firstDeliveryTriggered = false;

    private Dictionary<int, bool> triggeredFlags = new Dictionary<int, bool>()
    {
        {60, false},  // ← replaced 210
        {10, false}   // ← replaced 90, removed 30
    };

    public GameTimer MissionManager;

    [Header("Player score")]
    [Header("RBC")]
    public float comptTime = 0;
    public int performanceScore = 0;
    public int idleTime = 0;
    public int FailedDelivery = 0;
    [Header("WBCE")]
    public int EnemyDeathTime = 0;
    public int AttackableDied = 0;

    [Header("Timers")]
    public GameTimer missionTimer;
    public GameTimer missionTimer2;
    public GameTimer missionTimer3;

    [Header("Triggers")]
    public bool playerInTrigger = false;
    private bool sequenceRunning = false;

    [Header("CheatSystem")]
    public GameObject line;

    [Header("Mission Progress")]
    public int itemsDelivered = 0;
    public int deliveryThreshold = 5;
    public TMP_Text counterText;
    public bool hasCapturedTime = false;

    [Header("First Delivery Settings")]
    public bool ignoreFirstDelivery = true;   // toggle in Inspector
    private bool firstDeliverySkipped = false;

    [Header("Max Delivery Reached Event (Optional)")]
    [Tooltip("If enabled, onMaxDeliveryReached will fire once itemsDelivered hits deliveryThreshold.")]
    public bool enableMaxDeliveryEvent = false;
    [Tooltip("Assign any method(s) from any script here — runs once, the moment the delivery threshold is met.")]
    public UnityEvent onMaxDeliveryReached;
    [Tooltip("Optional. If assigned, this GameObject will be activated once the delivery threshold is met.")]
    public GameObject objectToActivateOnMaxDelivery;
    private bool maxDeliveryTriggered = false;

    [Header("Timer Reached Zero Event (Optional)")]
    [Tooltip("If enabled, onTimerReachedZero will fire once missionTimer3's time hits 0.")]
    public bool enableTimerZeroEvent = false;
    [Tooltip("Assign any method(s) from any script here — runs once, the moment missionTimer3 reaches 0.")]
    public UnityEvent onTimerReachedZero;
    private bool timerZeroTriggered = false;

    [Header("Values For Star (RBC)")]
    [Tooltip("ValuesForStar component to report into. The moment itemsDelivered equals " +
             "deliveryThreshold, this sends itemsDelivered into ValuesForStar's RBC field " +
             "(OxygenDeliver) - once, per attempt.")]
    public ValuesForStar valuesForStar;
    private bool oxygenDeliverReported = false;

    private float previousTime;

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Chapter1 - IRBC")
            StartCoroutine(dialogueSystem.DialogueSequence0IRBC());
        else if (sceneName == "Chapter1 - IWBCNM")
            StartCoroutine(dialogueSystem.DialogueSequence0IWBC());
        else if (sceneName == "Chapter1 - IWBCE")
            StartCoroutine(dialogueSystem.DialogueSequence1IWBCE());
        else if (sceneName == "Chapter1 - Platelets")
            StartCoroutine(dialogueSystem.DialogueSequenceIPI());
        else if (sceneName == "Chapter 1 - IICE")
            StartCoroutine(dialogueSystem.DialogueSequenceIICE0());
        else if (sceneName == "Chapter2 - Ascariasis")
            StartCoroutine(dialogueSystem.DialogueSequenceC2RBCA());
        else if (sceneName == "Chapter2 - Influenza")
            StartCoroutine(dialogueSystem.DialogueSequenceC2RBCI());
        else if (sceneName == "Chapter2 - Pneumonoccocal")
            StartCoroutine(dialogueSystem.DialogueSequenceC2RBCP());
        else if (sceneName == "Chapter2 - Dengue")
            StartCoroutine(dialogueSystem.DialogueSequenceC2RBCD());
        else if (sceneName == "Chapter2 - Malaria")
            StartCoroutine(dialogueSystem.DialogueSequenceC2RBCM());

        UpdateCounterUI();

        if (missionTimer != null)
            previousTime = missionTimer.GetCurrentTime();
    }

    void Update()
    {
        if (playerInTrigger && !sequenceRunning)
        {
            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName == "Chapter1 - IRBC")
                StartCoroutine(dialogueSystem.DialogueSequence0IRBC());
            else if (sceneName == "Chapter1 - IWBCNM")
                StartCoroutine(dialogueSystem.DialogueSequence0IWBC());
            else if (sceneName == "Chapter1 - IWBCE")
                StartCoroutine(dialogueSystem.DialogueSequence1IWBCE());
            else if (sceneName == "Chapter1 - Platelets")
                StartCoroutine(dialogueSystem.DialogueSequenceIPI());
            else if (sceneName == "Chapter 1 - IICE")
                StartCoroutine(dialogueSystem.DialogueSequenceIICE0());
            else if (sceneName == "Chapter2 - Ascariasis")
                StartCoroutine(dialogueSystem.DialogueSequenceC2RBCA());
            else if (sceneName == "Chapter2 - Influenza")
                StartCoroutine(dialogueSystem.DialogueSequenceC2RBCI());
            else if (sceneName == "Chapter2 - Pneumonoccocal")
                StartCoroutine(dialogueSystem.DialogueSequenceC2RBCP());
            else if (sceneName == "Chapter2 - Dengue")
                StartCoroutine(dialogueSystem.DialogueSequenceC2RBCD());
            else if (sceneName == "Chapter2 - Dengue")
                StartCoroutine(dialogueSystem.DialogueSequenceC2RBCM());

            playerInTrigger = false;
        }

        LogMissionTimer();
        ItemTimeChecker();
        CheckTimerReachedZero();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInTrigger = false;
    }

    public void UpdateCounterUI()
    {
        if (counterText != null)
            counterText.text = $"{itemsDelivered}/{deliveryThreshold}";
    }

    public void RegisterDelivery(int amount)
    {
        if (ignoreFirstDelivery && !firstDeliverySkipped)
        {
            firstDeliverySkipped = true;
            Debug.Log("[AI_TestTD] First delivery ignored — playing IRB9 dialogue.");

            if (missionTimer != null)
            {
                missionTimer.ActivateTimer();
                Debug.Log("[AI_TestTD] missionTimer started after first (ignored) delivery.");
            }

            StartCoroutine(dialogueSystem.DialogueSequenceIRB9());
            return;
        }

        itemsDelivered += amount;
        UpdateCounterUI();
        Debug.Log("[AI_TestTD] Items delivered: " + itemsDelivered);

        CheckMaxDeliveryReached();
        CheckAndReportOxygenDeliver();
    }

    private void CheckMaxDeliveryReached()
    {
        if (!enableMaxDeliveryEvent) return;
        if (maxDeliveryTriggered) return;

        if (itemsDelivered >= deliveryThreshold)
        {
            maxDeliveryTriggered = true;
            Debug.Log("[AI_TestTD] Max delivery reached — invoking onMaxDeliveryReached event.");
            onMaxDeliveryReached?.Invoke();

            // Optional: activate an additional GameObject when max delivery is reached.
            // Leave unassigned in the Inspector to skip.
            if (objectToActivateOnMaxDelivery != null)
                objectToActivateOnMaxDelivery.SetActive(true);
        }
    }

    // Sends itemsDelivered into ValuesForStar's RBC field (OxygenDeliver) the
    // moment itemsDelivered is EXACTLY equal to deliveryThreshold - only ever
    // once per attempt, guarded by oxygenDeliverReported. Uses an exact
    // equality check (not >=): if a single RegisterDelivery call can add more
    // than 1 item and overshoot deliveryThreshold in one step, this will not
    // fire that attempt. Call ResetOxygenDeliverReport() on level restart if
    // you need this to be able to fire again.
    private void CheckAndReportOxygenDeliver()
    {
        if (oxygenDeliverReported) return;
        if (valuesForStar == null) return;

        if (itemsDelivered == deliveryThreshold)
        {
            valuesForStar.ReportOxygenDeliver(itemsDelivered);
            oxygenDeliverReported = true;
            Debug.Log($"[AI_TestTD] itemsDelivered reached deliveryThreshold " +
                $"({itemsDelivered}/{deliveryThreshold}) - reported to ValuesForStar.OxygenDeliver.");
        }
    }

    /// <summary>Resets the one-shot RBC/OxygenDeliver report guard, e.g. when restarting the level/attempt.</summary>
    public void ResetOxygenDeliverReport()
    {
        oxygenDeliverReported = false;
    }

    private void CheckTimerReachedZero()
    {
        if (!enableTimerZeroEvent) return;
        if (timerZeroTriggered) return;
        if (missionTimer3 == null) return;

        if (missionTimer3.GetCurrentTime() <= 0f)
        {
            timerZeroTriggered = true;
            Debug.Log("[AI_TestTD] missionTimer3 reached 0 — invoking onTimerReachedZero event.");
            onTimerReachedZero?.Invoke();
        }
    }

    public void ItemTimeChecker()
    {
        if (missionTimer != null && !hasCapturedTime)
        {
            if (itemsDelivered >= deliveryThreshold)
            {
                hasCapturedTime = true;
                float currentTime = missionTimer.GetCurrentTime();
                comptTime = missionTimer.missionTime - currentTime;
                Debug.Log("Time elapsed: " + comptTime);
            }
        }
    }

    private void LogMissionTimer()
    {
        if (missionTimer == null) return;

        float currentTime = missionTimer.GetCurrentTime();

        EvaluateCheckpoint(60, previousTime, currentTime);
        EvaluateCheckpoint(10, previousTime, currentTime);

        previousTime = currentTime;
    }

    private void EvaluateCheckpoint(int triggerTime, float prevTime, float currentTime)
    {
        if (triggeredFlags[triggerTime]) return;

        if (prevTime > triggerTime && currentTime <= triggerTime)
        {
            triggeredFlags[triggerTime] = true;

            float score = EvaluateHeuristic(triggerTime, currentTime, itemsDelivered);
            int finalScore = Mathf.RoundToInt(score);

            performanceScore += finalScore;

            Debug.Log($"[AI] Triggered {triggerTime} | Score: {finalScore}");

            PlayDialogue(triggerTime, finalScore);
        }
    }

    private float EvaluateHeuristic(int triggerTime, float currentTime, int items)
    {
        float itemScore = items / 5f;
        float timeError = Mathf.Abs(currentTime - triggerTime);
        float timeScore = 1f - Mathf.Clamp01(timeError / 2f);
        float result = (itemScore * 0.7f) + (timeScore * 0.3f);
        return result * 5f;
    }

    private void PlayDialogue(int triggerTime, int score)
    {
        int tier = GetTier(score);

        switch (triggerTime)
        {
            case 60: StartCoroutine(Dialogue60(tier)); break;
            case 10: StartCoroutine(Dialogue10(tier)); break;
        }
    }

    private int GetTier(int score)
    {
        if (score >= 5) return 5;
        if (score >= 4) return 4;
        if (score >= 3) return 3;
        if (score >= 2) return 2;
        if (score >= 1) return 1;
        return 0;
    }

    private IEnumerator Dialogue60(int tier)
    {
        switch (tier)
        {
            case 5: yield return dialogueSystem.DialogueSequenceIRBCT2105(); break;
            case 4: yield return dialogueSystem.DialogueSequenceIRBCT2104(); break;
            case 3: yield return dialogueSystem.DialogueSequenceIRBCT2103(); break;
            case 2: yield return dialogueSystem.DialogueSequenceIRBCT2102(); break;
            case 1: yield return dialogueSystem.DialogueSequenceIRBCT2101(); break;
            default: yield return dialogueSystem.DialogueSequenceIRBCT2100(); break;
        }
    }

    private IEnumerator Dialogue10(int tier)
    {
        switch (tier)
        {
            case 5: yield return dialogueSystem.DialogueSequenceIRBCT305(); break;
            case 4: yield return dialogueSystem.DialogueSequenceIRBCT304(); break;
            case 3: yield return dialogueSystem.DialogueSequenceIRBCT303(); break;
            case 2: yield return dialogueSystem.DialogueSequenceIRBCT302(); break;
            case 1: yield return dialogueSystem.DialogueSequenceIRBCT301(); break;
            default: yield return dialogueSystem.DialogueSequenceIRBCT300(); break;
        }
    }
}