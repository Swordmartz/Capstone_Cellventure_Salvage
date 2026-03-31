using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AIGuideDecisionTree : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        DisableTarget,
        PlayDialogue,
        EnableGuide,
        PostDialogueActions,
        Completed
    }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public Image characterSprite;
    public TMP_Text dialogueText;
    public Button nextButton;

    [Header("Dialogue Settings")]
    public float lettersPerSecond = 30f;

    [Header("Post-Dialogue Target")]
    public GameObject targetObject; // Original post-dialogue target

    [Header("Guide system")]
    public Transform guideMark;     // The dynamic location mark in scene
    public Transform player;        // Player transform
    public LineRenderer guideLine;  // Line renderer for dotted trail
    public LayerMask obstacleMask;  // Layer mask for walls or obstacles
    public float minDistance = 1f;  // Hide line if player too close
    public float maxDistance = 10f; // Show line if player far enough
    public bool guideEnabled = true; // Can be disabled by other scripts

    [System.Serializable]
    public class DialogueLine
    {
        public string message;
        public Sprite characterSprite;
    }

    [System.Serializable]
    public class DialogueSet
    {
        public string setName;
        public List<DialogueLine> lines = new List<DialogueLine>();
    }

    public List<DialogueSet> dialogueSets = new List<DialogueSet>();

    [Header("Trigger Settings")]
    public GameObject targetGameObject; // GameObject1
    public GameObject oxygen;           // GameObject2, inactive at start
    public string dialogueSetName;      // Name of the dialogue set to play

    // ------------------ Internal ------------------
    private int currentMessage = 0;
    private Coroutine typingCoroutine;
    private DialogueSet activeDialogueSet;
    public bool dialogueFinished = false;

    private AIState currentState = AIState.Idle;

    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.AddListener(OnNextButton);
        }

        if (targetObject != null) targetObject.SetActive(false);
        if (guideLine != null) guideLine.enabled = false;
    }

    void Update()
    {
        // ------------------ Guide Line Logic ------------------
        if (dialogueFinished && guideEnabled && guideMark != null && player != null && guideLine != null)
        {
            float distance = Vector3.Distance(player.position, guideMark.position);
            bool blocked = Physics.Linecast(player.position, guideMark.position, obstacleMask);

            if (!blocked && distance >= minDistance && distance <= maxDistance)
            {
                guideLine.enabled = true;
                guideLine.positionCount = 2;
                guideLine.SetPosition(0, player.position);
                guideLine.SetPosition(1, guideMark.position);
            }
            else
            {
                guideLine.enabled = false;
            }
        }
        else if (guideLine != null)
        {
            guideLine.enabled = false;
        }

        // ------------------ Decision Tree Logic ------------------
        switch (currentState)
        {
            case AIState.Idle:
                break; // Waiting for trigger

            case AIState.DisableTarget:
                if (targetGameObject != null) targetGameObject.SetActive(false);
                currentState = AIState.PlayDialogue;
                break;

            case AIState.PlayDialogue:
                if (dialogueSets.Count > 0)
                {
                    TriggerDialogue(dialogueSetName);
                    currentState = AIState.EnableGuide;
                }
                else
                {
                    currentState = AIState.PostDialogueActions;
                }
                break;

            case AIState.EnableGuide:
                // Wait until dialogue finishes
                if (dialogueFinished)
                    currentState = AIState.PostDialogueActions;
                break;

            case AIState.PostDialogueActions:
                guideEnabled = true;
                if (oxygen != null)
                {
                    oxygen.SetActive(true);
                    guideMark = oxygen.transform;
                }
                if (targetGameObject != null)
                    targetGameObject.SetActive(true);

                currentState = AIState.Completed;
                break;

            case AIState.Completed:
                break; // AI sequence finished
        }
    }

    // ------------------ Dialogue System ------------------
    public void TriggerDialogue(string setName)
    {
        DialogueSet set = dialogueSets.Find(d => d.setName == setName);

        if (set != null && set.lines.Count > 0)
        {
            activeDialogueSet = set;
            currentMessage = 0;
            dialogueFinished = false;

            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            ShowNextMessage();
        }
        else
        {
            Debug.LogWarning($"Dialogue set '{setName}' not found or empty.");
            dialogueFinished = true;
        }
    }

    void ShowNextMessage()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (activeDialogueSet != null && currentMessage < activeDialogueSet.lines.Count)
        {
            DialogueLine line = activeDialogueSet.lines[currentMessage];

            if (line.characterSprite != null && characterSprite != null)
                characterSprite.sprite = line.characterSprite;

            typingCoroutine = StartCoroutine(TypeText(line.message));
            currentMessage++;

            if (nextButton != null) nextButton.gameObject.SetActive(true);
        }
        else
        {
            // Dialogue finished
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            activeDialogueSet = null;
            dialogueFinished = true;

            if (targetObject != null) targetObject.SetActive(true);
        }
    }

    private IEnumerator TypeText(string message)
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

    // ------------------ Public Trigger ------------------
    public void TriggerAISequence()
    {
        if (currentState == AIState.Idle || currentState == AIState.Completed)
            currentState = AIState.DisableTarget;
    }
}