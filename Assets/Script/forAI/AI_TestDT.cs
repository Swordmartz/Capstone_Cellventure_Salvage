using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


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
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Chapter1 - IRBC")
        {
            StartCoroutine(DialogueSequence0IRBC());
        }
        else if (sceneName == "Chapter1 - IWBC")
        {
            StartCoroutine(DialogueSequence0IWBC());
        }
        else
        {
            Debug.Log("no");
        }

    }

    void Update()
    {
        // Example: detect trigger to advance sequence
        if (playerInTrigger && !sequenceRunning)
        {
            StartCoroutine(DialogueSequence0IRBC());
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
    public IEnumerator DialogueSequence0IRBC()
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
    public IEnumerator DialogueSequence1IRBC()
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
    public IEnumerator DialogueSequence2IRBC()
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
    public IEnumerator DialogueSequence3IRBC()
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

    //-----------CORE SYSTEM for WBC----------------
    public IEnumerator DialogueSequence0IWBC()
    {
        // 1️⃣ Disable the assigned GameObject
        if (dialogueSystem.targetObject != null)
            dialogueSystem.targetObject.SetActive(false);

        // 2️⃣ Reactivate the dialogue button and panel right away
        if (dialogueSystem.dialoguePanel != null)
            dialogueSystem.dialoguePanel.SetActive(true);

        if (dialogueSystem.nextButton != null) // assuming you have a button reference
            dialogueSystem.nextButton.gameObject.SetActive(true);


        // 3️⃣ Play the first dialogue set (index 0)
        if (dialogueSystem != null && dialogueSystem.dialogueSets.Count > 0)
        {
            dialogueSystem.dialogueFinished = false;

            // Trigger dialogue at index 0
            dialogueSystem.TriggerDialogue(dialogueSystem.dialogueSets[0].setName);

            // Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueSystem.dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 0 not found or dialogueSystem is null!");
        }

        // 4️⃣ Reactivate the target object
        if (dialogueSystem.targetObject != null)
            dialogueSystem.targetObject.SetActive(true);
    }


}