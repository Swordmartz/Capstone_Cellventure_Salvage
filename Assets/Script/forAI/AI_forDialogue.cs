using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIforDialogue : MonoBehaviour
{
    [Header("UI References")]
    public AIforGuide guideSystem;
    public GameObject MB;
    public GameObject dialoguePanel;
    public Animator characterAnimator;
    public TMP_Text dialogueText;
    public Button nextButton;
    public GameTimer missionTimer; // ✅ drag your GameTimer object here
    public GameObject oxygen;
    public LayerMask enemyLayer;
    public GameObject CL;

    [Header("Intro Teleport")]
    public Transform introTeleportTarget;

    [Header("Layers To Hide (MULTI-SELECT IN INSPECTOR)")]
    public LayerMask layersToHide;

    [Header("Dialogue Settings")]
    public float lettersPerSecond = 30f;

    [Header("Optional Target")]
    public GameObject targetObject;

    [Header("CheatSystem")]
    public GameObject line;

    [System.Serializable]
    public class DialogueLine
    {
        public string message;
        public Animator characterAnimator;
    }

    [System.Serializable]
    public class DialogueSet
    {
        public string setName;
        public List<DialogueLine> lines = new List<DialogueLine>();
    }

    public List<DialogueSet> dialogueSets = new List<DialogueSet>();

    private int currentMessage = 0;
    private Coroutine typingCoroutine;
    private DialogueSet activeDialogueSet;
    public bool dialogueFinished = false;

    [Header("Sequence Target")]
    public GameObject targetGameObject;

    // ------------------ Setup ------------------

    // ------------------ Dialogue Core ------------------

    public void TriggerDialogue(string setName)
    {
        DialogueSet set = dialogueSets.Find(d => d.setName == setName);

        if (set != null && set.lines.Count > 0)
        {
            activeDialogueSet = set;
            currentMessage = 0;

            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            ShowNextMessage();
        }
        else
        {
            Debug.LogWarning($"Dialogue set '{setName}' not found or empty.");
        }
    }

    void ShowNextMessage()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (activeDialogueSet != null && currentMessage < activeDialogueSet.lines.Count)
        {
            DialogueLine line = activeDialogueSet.lines[currentMessage];

            if (line.characterAnimator != null && characterAnimator != null)
                characterAnimator.runtimeAnimatorController = line.characterAnimator.runtimeAnimatorController;

            typingCoroutine = StartCoroutine(TypeText(line.message));
            currentMessage++;

            if (nextButton != null)
                nextButton.gameObject.SetActive(true);
        }
        else
        {
            // ✅ Dialogue finished
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            activeDialogueSet = null;
            dialogueFinished = true;

            if (targetObject != null)
                targetObject.SetActive(true);
        }
    }

    IEnumerator TypeText(string message)
    {
        if (dialogueText == null) yield break;

        dialogueText.text = "";

        foreach (char c in message)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(1f / lettersPerSecond);
        }

        typingCoroutine = null;
    }

    public void OnNextButton()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = activeDialogueSet.lines[currentMessage - 1].message;
            typingCoroutine = null;
        }
        else
        {
            ShowNextMessage();
        }
    }
    public IEnumerator WaitForDialogue(System.Action onFinished)
    {
        // Wait until the current dialogue set is done
        while (activeDialogueSet != null)
            yield return null;

        onFinished?.Invoke();
    }
    public IEnumerator DialogueSequence0IRBC()
    {

        // Step 1: Disable target object
        if (targetGameObject != null) 
            targetGameObject.SetActive(false);
        if (MB != null) 
        {
            MB.SetActive(true); dialoguePanel.SetActive(false);
        }
        if (line != null)
            line.SetActive(false);

        yield return new WaitForSeconds(5f);

        if (MB != null) 
            MB.SetActive(false);

        // Step 2: Play dialogue index 0
        if (dialogueSets.Count > 0)
        {
            dialogueFinished = false;
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            TriggerDialogue(dialogueSets[0].setName);
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 0 not found or dialogueSystem is null!");
        }

        // Step 3: Teleport player BEFORE reactivating
        if (introTeleportTarget != null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Rigidbody rb = playerObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = introTeleportTarget.position;
                }
                else
                {
                    playerObj.transform.position = introTeleportTarget.position;
                }
                Debug.Log("Player teleported to " + introTeleportTarget.name);
            }
        }

        // Step 3.5: Play dialogue index 1 after teleport
        if (dialogueSets.Count > 22)
        {
            dialogueFinished = false;
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            TriggerDialogue(dialogueSets[22].setName);
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 22 not found!");
        }

        // Step 4: Reactivate target object and line
        if (targetGameObject != null) targetGameObject.SetActive(true);
        if (line != null) line.SetActive(true);

        // Step 5: Activate mission timer
        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }

       

    public IEnumerator DialogueSequence1IRBC()
    {
        if (missionTimer != null) 
            missionTimer.StopTimer();
        // 1️⃣ Reset joystick input BEFORE disabling the GameObject
        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play the second dialogue set (index 1)
        if (dialogueSets.Count > 1)
        {
            bool localDialogueFinished = false;

            TriggerDialogue(dialogueSets[1].setName);

            StartCoroutine(WaitForDialogue(() => localDialogueFinished = true));
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
        if (targetGameObject != null)
            targetGameObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }
    public IEnumerator DialogueSequence2IRBC()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();
        // 1️⃣ Disable GameObject
        if (targetGameObject != null)
            targetGameObject.SetActive(false);

        // 2️⃣ Play the THIRD dialogue set
        if (dialogueSets.Count > 2)
        {
            TriggerDialogue(dialogueSets[2].setName);

            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 3 (index 2) not found!");
        }

        // 4️⃣ Reactivate GameObject
        if (targetGameObject != null)
            targetGameObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }
    public IEnumerator DialogueSequence3IRBC()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();
        // 1️⃣ Disable GameObject
        if (targetGameObject != null)
            targetGameObject.SetActive(false);

        // 2️⃣ Reset dialogue flag
        dialogueFinished = false;

        // 3️⃣ Play the FOURTH dialogue set (index 3)
        if (dialogueSets.Count > 3)
        {
            TriggerDialogue(dialogueSets[3].setName);

            // 4️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 4 (index 3) not found!");
        }

        // 5️⃣ Reactivate GameObject
        if (targetGameObject != null)
            targetGameObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }
    // Helper coroutine to track when a dialogue finishes

    //-----------CORE SYSTEM for WBC----------------
    public IEnumerator DialogueSequence0IWBC()
    {
        // 1️⃣ Disable the assigned GameObject
        if (targetObject != null)
            targetObject.SetActive(false);

        // 2️⃣ Reactivate the dialogue button and panel right away
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        // 3️⃣ Play the first dialogue set (index 0)
        if (dialogueSets.Count > 0)
        {
            dialogueFinished = false;

            // Trigger dialogue at index 0
            TriggerDialogue(dialogueSets[0].setName);

            // Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 0 not found or dialogueSystem is null!");
        }

        // 4️⃣ Reactivate the target object
        if (targetObject != null)
            targetObject.SetActive(true);

        // 5 Activate the mission timer
        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }

    public IEnumerator DialogueSequence1IWBC()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();
        // 1️⃣ Disable the first object
        if (targetObject != null)
            targetObject.SetActive(false);

        // 2️⃣ Play dialogue at index 1
        if (dialogueSets.Count > 1)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[1].setName);

            // Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 1 not found or dialogueSystem is null!");
        }

        // 3️⃣ Activate all GameObjects in the enemy layer
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & enemyLayer) != 0)
            {
                obj.SetActive(true);
                Debug.Log("Activated enemy: " + obj.name);
            }
        }



        // 4️⃣ Re‑enable the first object
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }

    public IEnumerator DialogueSequenceIRBCT2105()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 4)
        {

            dialogueFinished = false;
            TriggerDialogue(dialogueSets[4].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }

    public IEnumerator DialogueSequenceIRBCT2104()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);

        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 5)
        {

            dialogueFinished = false;
            TriggerDialogue(dialogueSets[5].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT2103()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }
        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 6)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[6].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT2102()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 7)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[7].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT2101()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 8)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[8].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT2100()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 9)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[9].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT905()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 10)
        {

            dialogueFinished = false;
            TriggerDialogue(dialogueSets[10].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }

    public IEnumerator DialogueSequenceIRBCT904()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 11)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[11].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT903()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 12)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[12].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT902()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 13)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[13].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT901()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 14)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[14].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT900()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 15)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[15].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();


    }
    public IEnumerator DialogueSequenceIRBCT305()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 16)
        {

            dialogueFinished = false;
            TriggerDialogue(dialogueSets[16].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT304()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 17)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[17].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT303()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 18)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[18].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT302()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 19)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[19].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT301()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 20)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[20].setName);


            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBCT300()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 21)
        {

            dialogueFinished = false;
            TriggerDialogue(dialogueSets[21].setName);

            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();


    }
    public IEnumerator DialogueSequenceIRBCTPA()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 24)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[24].setName);

            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRBC05()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 23)
        {

            dialogueFinished = false;
            TriggerDialogue(dialogueSets[23].setName);

            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();

    }
    public IEnumerator DialogueSequenceIRB5()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 29)
        {

            dialogueFinished = false;
            TriggerDialogue(dialogueSets[29].setName);
            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }

    public IEnumerator DialogueSequenceIRB66()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 25)
        {

            dialogueFinished = false;
            TriggerDialogue(dialogueSets[25].setName);
            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }

    public IEnumerator DialogueSequenceIRB7()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null);
            }

            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // Play dialogue
        if (dialogueSets.Count > 26)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[26].setName);
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // Re-enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        // Show menu after dialogue finishes
        if (CL != null)
            CL.SetActive(true);
        else
            Debug.LogWarning("menuGameObject is not assigned!");

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }

    public IEnumerator DialogueSequenceIRB8()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 27)
        {

            dialogueFinished = false;
            TriggerDialogue(dialogueSets[27].setName);
            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }

    public IEnumerator DialogueSequenceIRB9()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetGameObject != null)
        {
            // If your joystick is a VariableJoystick/FixedJoystick from the Joystick Pack:
            Joystick joystick = targetGameObject.GetComponentInChildren<Joystick>();
            if (joystick != null)
            {
                joystick.OnPointerUp(null); // Force-release the joystick
            }

            // Small delay to let input system process the release
            yield return new WaitForEndOfFrame();

            targetGameObject.SetActive(false);
        }

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 28)
        {

            dialogueFinished = false;
            TriggerDialogue(dialogueSets[28].setName);
            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the GameObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }
    public IEnumerator DialogueSequence0IWBCE()
    {
        Camera cam = Camera.main;
        if (cam == null)
            yield break;

        // 1️⃣ Hide MULTIPLE layers at once (Inspector controlled)
        cam.cullingMask = cam.cullingMask & ~layersToHide.value;

        // 2️⃣ Disable target object
        if (targetObject != null)
        {
            targetObject.SetActive(false);
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        // 3️⃣ Play dialogue index 0
        if (dialogueSets != null && dialogueSets.Count > 0)
        {
            dialogueFinished = false;

            TriggerDialogue(dialogueSets[0].setName);

            yield return new WaitUntil(() => dialogueFinished);
        }

        // 4️⃣ Re-enable target object
        if (targetObject != null)
            targetObject.SetActive(true);

        // 5️⃣ 🔥 ACTIVATE ALL ENEMIES (WORKS EVEN IF INACTIVE)
        EnemyFSM[] enemies = Resources.FindObjectsOfTypeAll<EnemyFSM>();

        foreach (EnemyFSM enemy in enemies)
        {
            if (enemy == null) continue;

            // optional safety: avoid prefab assets
            if (enemy.gameObject.scene.IsValid())
            {
                enemy.gameObject.SetActive(true);
                Debug.Log("Activated enemy: " + enemy.name);
            }
        }
    }
    public IEnumerator DialogueSequenceIPI()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        // 1️⃣ Disable the targetObject
        if (targetObject != null)
            targetObject.SetActive(false);

        // 2️⃣ Play dialogue
        if (dialogueSets.Count > 0)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[0].setName);

            // 3️⃣ Wait until dialogue finishes
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("DialogueSystem is not assigned!");
        }

        // 4️⃣ Re‑enable the targetObject
        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ResumeTimer();
    }

}

