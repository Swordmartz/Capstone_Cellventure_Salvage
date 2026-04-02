using UnityEngine;
using System.Collections;

public class AI_TestTD : MonoBehaviour
{
    [Header("Reference")]
    public AIforDialogue dialogueSystem;  // Your dialogue script
    public AIforGuide guideSystem; // Your guide script
    public GameObject oxygen;

    [Header("Triggers")]
    public bool playerInTrigger = false;

    private bool sequenceRunning = false;

    [Header("CheatSystem")]
    public GameObject line;


    void Start()
    {
        // 🔥 Start the dialogue sequence automatically
            StartCoroutine(HandleTriggerSequence0());
        
    }

    void Update()
    {
        // Example: detect trigger to advance sequence
        if (playerInTrigger && !sequenceRunning)
        {
            StartCoroutine(HandleTriggerSequence0());
            playerInTrigger = false; // reset trigger
        }
    }

    // ---------------- TRIGGER HANDLERS ----------------
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

    // ---------------- CORE SEQUENCE ----------------
    public IEnumerator HandleTriggerSequence0()
    {
        // ------------------- Step 1: Disable target object if assigned -------------------
        if (dialogueSystem.targetGameObject != null)
        {
            dialogueSystem.targetGameObject.SetActive(false);

        }
        if (line != null)
        {
            line.SetActive(false);
        }

        // ------------------- Step 2: Play dialogue index 0 -------------------
        if (dialogueSystem != null && dialogueSystem.dialogueSets.Count > 0)
        {
            // Reset dialogue finished flag before starting
            dialogueSystem.dialogueFinished = false;

            // Ensure dialogue panel is active immediately
            if (dialogueSystem.dialoguePanel != null)
                dialogueSystem.dialoguePanel.SetActive(true);

            // Trigger the first dialogue
            dialogueSystem.TriggerDialogue(dialogueSystem.dialogueSets[0].setName);

            // Wait until the dialogue finishes
            yield return new WaitUntil(() => dialogueSystem.dialogueFinished);
        }
       
        else
        {
            Debug.LogWarning("Dialogue Set 0 not found or dialogueSystem is null!");
        }

        // ------------------- Step 3: Reactivate target object if assigned -------------------
        if (dialogueSystem.targetGameObject != null)
        {
            dialogueSystem.targetGameObject.SetActive(true);

        }
        if (line != null)
        {
            line.SetActive(true);
        }
    }
    public IEnumerator HandleTriggerSequence1()
    {
        // 1️⃣ Immediately disable GameObject1
        if (dialogueSystem.targetGameObject != null)
            dialogueSystem.targetGameObject.SetActive(false);

        // 2️⃣ Play the second dialogue set (index 1)
        if (dialogueSystem.dialogueSets.Count > 1)
        {
            // Reset dialogueFinished locally for this set
            bool localDialogueFinished = false;

            // Trigger second dialogue
            dialogueSystem.TriggerDialogue(dialogueSystem.dialogueSets[1].setName);

            // Wait until the dialogue finishes
            StartCoroutine(dialogueSystem.WaitForDialogue(() => localDialogueFinished = true));
            while (!localDialogueFinished)
                yield return null;
        }

        // 3️⃣ Reactivate guide system
        guideSystem.guideEnabled = true;

        // 4️⃣ Activate GameObject2 (oxygen) and switch guideMark
        if (oxygen != null)
        {
            oxygen.SetActive(true);
            guideSystem.guideMark = oxygen.transform;
        }

        // 5️⃣ Re-enable GameObject1
        if (dialogueSystem.targetGameObject != null)
            dialogueSystem.targetGameObject.SetActive(true);
    }
    public IEnumerator HandleTriggerSequence2()
    {
        // 1️⃣ Disable GameObject
        if (dialogueSystem.targetGameObject != null)
            dialogueSystem.targetGameObject.SetActive(false);

        // 2️⃣ Play the THIRD dialogue set
        if (dialogueSystem.dialogueSets.Count > 2)
        {
            dialogueSystem.TriggerDialogue(dialogueSystem.dialogueSets[2].setName);

            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueSystem.dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 3 (index 2) not found!");
        }

        // 4️⃣ Reactivate GameObject
        if (dialogueSystem.targetGameObject != null)
            dialogueSystem.targetGameObject.SetActive(true);
    }
    public IEnumerator HandleTriggerSequence3()
    {
        // 1️⃣ Disable GameObject
        if (dialogueSystem.targetGameObject != null)
            dialogueSystem.targetGameObject.SetActive(false);

        // 2️⃣ Reset dialogue flag
        dialogueSystem.dialogueFinished = false;

        // 3️⃣ Play the FOURTH dialogue set (index 3)
        if (dialogueSystem.dialogueSets.Count > 3)
        {
            dialogueSystem.TriggerDialogue(dialogueSystem.dialogueSets[3].setName);

            // 4️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueSystem.dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 4 (index 3) not found!");
        }

        // 5️⃣ Reactivate GameObject
        if (dialogueSystem.targetGameObject != null)
            dialogueSystem.targetGameObject.SetActive(true);
    }
    // Helper coroutine to track when a dialogue finishes

}