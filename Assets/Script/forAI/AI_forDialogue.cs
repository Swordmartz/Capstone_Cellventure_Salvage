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
    public GameTimer missionTimer;
    public GameObject[] oxygen;
    public LayerMask enemyLayer;
    public GameObject CL;

    [Header("Audio")]
    public AudioSource dialogueAudioSource;

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
        public AudioClip audioClip; // 🔊 Per-line audio
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

        // Stop any playing audio when moving to next line
        if (dialogueAudioSource != null && dialogueAudioSource.isPlaying)
            dialogueAudioSource.Stop();

        if (activeDialogueSet != null && currentMessage < activeDialogueSet.lines.Count)
        {
            DialogueLine line = activeDialogueSet.lines[currentMessage];

            if (line.characterAnimator != null && characterAnimator != null)
                characterAnimator.runtimeAnimatorController = line.characterAnimator.runtimeAnimatorController;

            // 🔊 Play the audio clip for this line
            if (dialogueAudioSource != null && line.audioClip != null)
            {
                dialogueAudioSource.clip = line.audioClip;
                dialogueAudioSource.Play();
            }

            typingCoroutine = StartCoroutine(TypeText(line.message));
            currentMessage++;

            if (nextButton != null)
                nextButton.gameObject.SetActive(true);
        }
        else
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            // Stop audio when dialogue ends
            if (dialogueAudioSource != null && dialogueAudioSource.isPlaying)
                dialogueAudioSource.Stop();

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
            // Skip typing — stop audio immediately
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

            if (dialogueAudioSource != null && dialogueAudioSource.isPlaying)
                dialogueAudioSource.Stop();

            dialogueText.text = activeDialogueSet.lines[currentMessage - 1].message;
        }
        else
        {
            ShowNextMessage();
        }
    }

    public IEnumerator WaitForDialogue(System.Action onFinished)
    {
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
        if (oxygen != null && oxygen.Length > 0)
        {
            foreach (GameObject obj in oxygen)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
            guideSystem.guideMark = oxygen[0].transform;
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
        // 1. Disable the assigned GameObject
        if (targetObject != null)
            targetObject.SetActive(false);
        if (MB != null)
        {
            MB.SetActive(true); dialoguePanel.SetActive(false);
        }
        if (line != null)
            line.SetActive(false);

        yield return new WaitForSeconds(5f);

        if (MB != null)
            MB.SetActive(false);

        // 2. Activate the dialogue panel and next button
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        // 3. Play the first dialogue set, index 0
        if (dialogueSets.Count > 0)
        {
            dialogueFinished = false;

            TriggerDialogue(dialogueSets[0].setName);

            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 0 not found!");
        }

        // 4. Wait before teleport
        yield return null;
        yield return new WaitForFixedUpdate();

        // 5. Teleport player using Transform only
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null && introTeleportTarget != null)
        {
            playerObj.transform.position = introTeleportTarget.position;

            Debug.Log("Player teleported to " + introTeleportTarget.position);
        }
        else
        {
            Debug.LogWarning("Player or Intro Teleport Target is missing!");
        }



        // 6. Play dialogue set index 2 after teleport
        if (dialogueSets.Count > 2)
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            if (nextButton != null)
                nextButton.gameObject.SetActive(true);

            dialogueFinished = false;

            TriggerDialogue(dialogueSets[2].setName);

            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 2 not found!");
        }

        // 7. Only reactivate target object after ALL dialogues are finished
        if (targetObject != null)
            targetObject.SetActive(true);

        // 8. Only resume mission timer after ALL dialogues are finished
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
        // Disable target object
        if (targetObject != null)
            targetObject.SetActive(false);

        if (line != null)
            line.SetActive(false);

        yield return new WaitForSeconds(5f);

        if (MB != null)
            MB.SetActive(false);

        // Play dialogue
        if (dialogueSets != null && dialogueSets.Count > 0)
        {
            dialogueFinished = false;

            TriggerDialogue(dialogueSets[0].setName);

            yield return new WaitUntil(() => dialogueFinished);
        }

        // Re-enable target object
        if (targetObject != null)
            targetObject.SetActive(true);

        // Activate all enemies
        EnemyFSM[] enemies = Resources.FindObjectsOfTypeAll<EnemyFSM>();

        foreach (EnemyFSM enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.scene.IsValid())
            {
                enemy.gameObject.SetActive(true);
                Debug.Log("Activated enemy: " + enemy.name);
            }
        }

        if (missionTimer != null)
            missionTimer.ActivateTimer();
    }
    public IEnumerator DialogueSequenceIPI()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        // 1️⃣ Disable the targetObject
        if (targetObject != null)
            targetObject.SetActive(false);

        if (MB != null)
        {
            MB.SetActive(true); dialoguePanel.SetActive(false);
        }
        if (line != null)
            line.SetActive(false);

        yield return new WaitForSeconds(5f);

        if (MB != null)
            MB.SetActive(false);

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
            missionTimer.ActivateTimer();
    }

    public IEnumerator DialogueSequenceIICE0()
    {

        if (missionTimer != null)
            missionTimer.StopTimer();

        if (targetObject != null)
            targetObject.SetActive(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        if (dialogueSets.Count > 0)
        {
            dialogueFinished = false;

            TriggerDialogue(dialogueSets[0].setName);

            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 0 not found!");
        }

        // Local search for inactive GameObjects with tag ICEVIT
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag("ICEVIT") && obj.scene.IsValid())
            {
                obj.SetActive(true);
            }
        }

        if (targetObject != null)
            targetObject.SetActive(true);

        if (missionTimer != null)
            missionTimer.ActivateTimer();
    }

    public IEnumerator DialogueSequence1IWBCE()
    {
        if (missionTimer != null)
            missionTimer.StopTimer();

        // 1) Disable the target object
        if (targetObject != null)
            targetObject.SetActive(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        // 2) Play dialogue index 1
        if (dialogueSets.Count > 1)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[1].setName);
            yield return new WaitUntil(() => dialogueFinished);
            dialogueFinished = false; // Reset so index 2 wait works correctly

            // Force disable immediately after ShowNextMessage reactivates it
            if (targetObject != null)
                targetObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 1 not found!");
        }

        // 3) Teleport player — object stays disabled
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && introTeleportTarget != null)
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
        else
        {
            Debug.LogWarning("Player or introTeleportTarget is missing!");
        }

        // Force disable again after teleport
        if (targetObject != null)
            targetObject.SetActive(false);

        // 4) Play dialogue index 2
        if (dialogueSets.Count > 2)
        {
            dialogueFinished = false;
            TriggerDialogue(dialogueSets[2].setName);
            yield return new WaitUntil(() => dialogueFinished);
        }
        else
        {
            Debug.LogWarning("Dialogue Set 2 not found!");
        }

        // 5) Start the timer
        if (missionTimer != null)
            missionTimer.ActivateTimer();

        // 6) Activate the target object
        if (targetObject != null)
            targetObject.SetActive(true);

        // Activate ICEVIT tagged objects
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag("ICEVIT") && obj.scene.IsValid())
                obj.SetActive(true);
        }
    }
}

