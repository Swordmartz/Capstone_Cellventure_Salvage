using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AI_Dialogue : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public Image characterSprite;
    public TMP_Text dialogueText;
    public Button nextButton;

    [Header("Dialogue Settings")]
    public float lettersPerSecond = 30f;

    [Header("Post-Dialogue Target")]
    public GameObject targetObject; // Original post-dialogue target

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

    private int currentMessage = 0;
    private Coroutine typingCoroutine;
    private DialogueSet activeDialogueSet;
    private bool dialogueFinished = false;

    void Start()
    {
        // Disable panel at start
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // Set up next button
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.AddListener(OnNextButton);
        }

        // Remove automatic dialogue start
        // TriggerDialogue(dialogueSets[0].setName); <-- removed
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

            // Notify post-dialogue object
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
}