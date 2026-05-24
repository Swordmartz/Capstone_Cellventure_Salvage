using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AI_TestTD : MonoBehaviour
{
    [Header("Reference")]
    public AIforDialogue dialogueSystem;
    public AIforGuide guideSystem;
    public GameObject oxygen;
    public LayerMask enemyLayer;
    public GameObject MB;
    private bool firstDeliveryTriggered = false; // ADD THIS

    private Dictionary<int, bool> triggeredFlags = new Dictionary<int, bool>()
    {
        {210, false},
        {90, false},
        {30, false}
    };

    public GameTimer MissionManager;

    [Header("Player score")]
    public float comptTime = 0;//When the goal is reached, the time is captured and stored in this variable
    public int performanceScore = 0;//For Tracking of Performance Score(The score is get by the decision tree)
    public int idleTime = 0; //For Tracking of Idle time
    public int FailedDelivery = 0; //For Tracking of Failed Delivery

    [Header("Triggers")]
    public bool playerInTrigger = false;
    private bool sequenceRunning = false;

    [Header("CheatSystem")]
    public GameObject line;

    [Header("Mission Progress")]
    public int itemsDelivered = 0;
    public int deliveryThreshold = 5;
    public TMP_Text counterText;
    public GameTimer missionTimer;
    public bool hasCapturedTime = false;



    // 🔥 NEW (for proper checkpoint detection)
    private float previousTime;

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Chapter1 - IRBC")
        {
            StartCoroutine(dialogueSystem.DialogueSequence0IRBC());
        }
        else if (sceneName == "Chapter1 - IWBCNM")
        {
            StartCoroutine(dialogueSystem.DialogueSequence0IWBC());
        }
        else if (sceneName == "Chapter1 - IWBCE")
        {
            StartCoroutine(dialogueSystem.DialogueSequence0IWBCE());
        }
        else if (sceneName == "Chapter1 - Platelets")
        {
            StartCoroutine(dialogueSystem.DialogueSequenceIPI());
        }
        else if (sceneName == "Chapter 1 - IICE")
        {
            StartCoroutine(dialogueSystem.DialogueSequenceIICE0());
        }

        UpdateCounterUI();

        // 🔥 INIT previous time
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

                StartCoroutine(dialogueSystem.DialogueSequence0IWBCE());
            else if (sceneName == "Chapter1 - Platelets")

                StartCoroutine(dialogueSystem.DialogueSequenceIPI());

            else if (sceneName == "Chapter 1 - IICE")
                StartCoroutine(dialogueSystem.DialogueSequenceIICE0()); 


            playerInTrigger = false;
        }

        LogMissionTimer();
        ItemTimeChecker();
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
        {
            counterText.text = $"{itemsDelivered}/{deliveryThreshold}";
        }
    }

    public void ItemTimeChecker()
    {
        if (missionTimer != null && !hasCapturedTime)
        {
            if (itemsDelivered >= 1 && !firstDeliveryTriggered)
            {
                firstDeliveryTriggered = true;
                StartCoroutine(dialogueSystem.DialogueSequenceIRB9()); // Replace with your target dialogue
            }
            if (itemsDelivered >= deliveryThreshold)
            {
                hasCapturedTime = true;

                float currentTime = missionTimer.GetCurrentTime();
                comptTime = missionTimer.missionTime - currentTime; // elapsed time

                Debug.Log("Time elapsed: " + comptTime);
            }
        }
    }

    // =========================
    // ⏱ TIMER CHECK (FIXED)
    // =========================
    private void LogMissionTimer()
    {
        if (missionTimer == null) return;

        float currentTime = missionTimer.GetCurrentTime();

        EvaluateCheckpoint(210, previousTime, currentTime);
        EvaluateCheckpoint(90, previousTime, currentTime);
        EvaluateCheckpoint(30, previousTime, currentTime);

        previousTime = currentTime; // 🔥 IMPORTANT
    }

    // =========================
    // 🧠 CHECKPOINT FIX
    // =========================
    private void EvaluateCheckpoint(int triggerTime, float prevTime, float currentTime)
    {
        // ❌ OLD: ContainsKey (always true)
        // ✅ FIX:
        if (triggeredFlags[triggerTime]) return;

        // 🔥 CROSSING DETECTION (MAIN FIX)
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

    // =========================
    // 🧠 HEURISTIC FUNCTION
    // =========================
    private float EvaluateHeuristic(int triggerTime, float currentTime, int items)
    {
        float itemScore = items / 5f; // ✅ was 4f, max deliveries is 5

        float timeError = Mathf.Abs(currentTime - triggerTime);
        float timeScore = 1f - Mathf.Clamp01(timeError / 2f);

        float result =
            (itemScore * 0.7f) +
            (timeScore * 0.3f);

        return result * 5f;
    }
    // =========================
    // 🎭 DIALOGUE SELECTOR
    // =========================
    private void PlayDialogue(int triggerTime, int score)
    {
        int tier = GetTier(score);

        switch (triggerTime)
        {
            case 210:
                StartCoroutine(Dialogue210(tier));
                break;

            case 90:
                StartCoroutine(Dialogue90(tier));
                break;

            case 30:
                StartCoroutine(Dialogue30(tier));
                break;
        }
    }

    // =========================
    // 🧠 SCORE → TIER
    // =========================
    private int GetTier(int score)
    {
        if (score >= 5) return 5;
        if (score >= 4) return 4;
        if (score >= 3) return 3;
        if (score >= 2) return 2;
        if (score >= 1) return 1;
        return 0;
    }

    // =========================
    // 🎭 DIALOGUES
    // =========================
    private IEnumerator Dialogue210(int tier)
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

    private IEnumerator Dialogue90(int tier)
    {
        switch (tier)
        {
            case 5: yield return dialogueSystem.DialogueSequenceIRBCT905(); break;
            case 4: yield return dialogueSystem.DialogueSequenceIRBCT904(); break;
            case 3: yield return dialogueSystem.DialogueSequenceIRBCT903(); break;
            case 2: yield return dialogueSystem.DialogueSequenceIRBCT902(); break;
            case 1: yield return dialogueSystem.DialogueSequenceIRBCT901(); break;
            default: yield return dialogueSystem.DialogueSequenceIRBCT900(); break;
        }
    }

    private IEnumerator Dialogue30(int tier)
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
