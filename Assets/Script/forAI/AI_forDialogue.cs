using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AIforDialogue : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public Image characterSprite;
    public TMP_Text dialogueText;
    public Button nextButton;

    [Header("Dialogue Settings")]
    public float lettersPerSecond = 30f;

    [Header("Optional Target")]
    public GameObject targetObject;

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
    public bool dialogueFinished = false;

    [Header("Sequence Target")]
    public GameObject targetGameObject;

    // ------------------ Setup ------------------
    void Start()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.AddListener(OnNextButton);
        }

        if (targetObject != null)
            targetObject.SetActive(false);
    }

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

            if (line.characterSprite != null && characterSprite != null)
                characterSprite.sprite = line.characterSprite;

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

}